using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text;
using Widgets.ExtendedWebApi.Services;

namespace Widgets.ExtendedWebApi.Controllers.Mobile;

/// <summary>
/// Account API for mobile app - registration and user management
/// </summary>
[ApiController]
[Route("api/mobile/[controller]")]
[Produces("application/json")]
public class AccountController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ICustomerManagerService _customerManagerService;
    private readonly IGroupService _groupService;
    private readonly IStoreContext _storeContext;
    private readonly CustomerSettings _customerSettings;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public AccountController(
        ICustomerService customerService,
        ICustomerManagerService customerManagerService,
        IGroupService groupService,
        IStoreContext storeContext,
        CustomerSettings customerSettings,
        IConfiguration configuration)
    {
        _customerService = customerService;
        _customerManagerService = customerManagerService;
        _groupService = groupService;
        _storeContext = storeContext;
        _customerSettings = customerSettings;
        _jwtTokenGenerator = new JwtTokenGenerator(configuration);
    }

    /// <summary>
    /// Login with customer credentials and get JWT token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null)
            return BadRequest(new { error = "Invalid request" });

        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email is required" });

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Password is required" });

        // Decode password if base64 encoded
        var password = request.Password;
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(request.Password));
            if (!string.IsNullOrEmpty(decoded))
                password = decoded;
        }
        catch
        {
            // Password was not base64 encoded, use as-is
        }

        try
        {
            // Validate customer credentials
            var loginResult = await _customerManagerService.LoginCustomer(request.Email, password);

            if (loginResult != CustomerLoginResults.Successful)
            {
                var errorMessage = loginResult switch
                {
                    CustomerLoginResults.WrongPassword => "Invalid email or password",
                    CustomerLoginResults.NotRegistered => "Account not found",
                    CustomerLoginResults.NotActive => "Account is not active",
                    CustomerLoginResults.Deleted => "Account has been deleted",
                    CustomerLoginResults.RequiresTwoFactor => "Two-factor authentication required",
                    _ => "Login failed"
                };
                return Unauthorized(new { error = errorMessage });
            }

            // Get customer
            var customer = await _customerService.GetCustomerByEmail(request.Email);
            if (customer == null)
                return Unauthorized(new { error = "Customer not found" });

            // Generate JWT token with customer claims
            var claims = new Dictionary<string, string>
            {
                { "Email", customer.Email },
                { "CustomerId", customer.Id },
                { "Guid", customer.CustomerGuid.ToString() }
            };

            var token = _jwtTokenGenerator.GenerateToken(claims);

            return Ok(new
            {
                token,
                customerId = customer.Id,
                email = customer.Email,
                firstName = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName),
                lastName = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName)
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Register a new customer account
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request == null)
            return BadRequest(new { error = "Invalid request" });

        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email is required" });

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Password is required" });

        // Check if registration is allowed
        if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
            return BadRequest(new { error = "Registration is disabled" });

        // Decode password if base64 encoded
        var password = request.Password;
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(request.Password));
            if (!string.IsNullOrEmpty(decoded))
                password = decoded;
        }
        catch
        {
            // Password was not base64 encoded, use as-is
        }

        // Validate password length
        if (password.Length < 6)
            return BadRequest(new { error = "Password must be at least 6 characters" });

        // Check if email is already registered
        var existingCustomer = await _customerService.GetCustomerByEmail(request.Email);
        if (existingCustomer != null)
            return Conflict(new { error = "This email is already registered" });

        try
        {
            // Get customer groups
            var registeredGroup = await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered);
            var guestGroup = await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Guests);

            // Create new customer
            var customer = new Customer
            {
                CustomerGuid = Guid.NewGuid(),
                Email = request.Email,
                Username = request.Email,
                Active = true,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };

            // Add to registered group
            if (registeredGroup != null)
                customer.Groups.Add(registeredGroup.Id);

            // Insert the customer first
            await _customerService.InsertCustomer(customer);

            // Set first name and last name as user fields
            if (!string.IsNullOrEmpty(request.FirstName))
                await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.FirstName, request.FirstName);
            if (!string.IsNullOrEmpty(request.LastName))
                await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.LastName, request.LastName);

            // Determine if customer should be auto-approved
            var isApproved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard;

            // Register the customer (this sets the password)
            var registrationRequest = new RegistrationRequest(
                customer,
                request.Email,
                request.Email,
                password,
                _customerSettings.DefaultPasswordFormat,
                _storeContext.CurrentStore.Id,
                isApproved
            );

            await _customerManagerService.RegisterCustomer(registrationRequest);

            // Return success with appropriate message based on registration type
            var message = _customerSettings.UserRegistrationType switch
            {
                UserRegistrationType.EmailValidation => "Registration successful. Please check your email to activate your account.",
                UserRegistrationType.AdminApproval => "Registration successful. Your account is pending approval.",
                _ => "Registration successful. You can now log in."
            };

            return Ok(new
            {
                success = true,
                message,
                customerId = customer.Id,
                email = customer.Email,
                requiresActivation = _customerSettings.UserRegistrationType == UserRegistrationType.EmailValidation,
                requiresApproval = _customerSettings.UserRegistrationType == UserRegistrationType.AdminApproval
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Check if an email is available for registration
    /// </summary>
    [HttpGet("check-email")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckEmailAvailability([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { error = "Email is required" });

        var existingCustomer = await _customerService.GetCustomerByEmail(email);
        return Ok(new { available = existingCustomer == null });
    }
}

/// <summary>
/// Login request model
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

/// <summary>
/// Registration request model
/// </summary>
public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
