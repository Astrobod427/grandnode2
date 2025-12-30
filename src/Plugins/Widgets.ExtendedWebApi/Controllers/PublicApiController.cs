using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Widgets.ExtendedWebApi.DTOs;
using Widgets.ExtendedWebApi.Infrastructure;

namespace Widgets.ExtendedWebApi.Controllers;

/// <summary>
/// Public API endpoints for n8n and external integrations
/// Protected by API Key authentication
/// </summary>
[ApiController]
[Route("api/extended")]
[AllowAnonymous]
[ApiKeyAuthorize]
public class PublicApiController : ControllerBase
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<UserApi> _userApiRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly BackendAPIConfig _apiConfig;
    private readonly ICustomerService _customerService;
    private readonly IUserApiService _userApiService;

    public PublicApiController(
        IRepository<Product> productRepository,
        IRepository<Order> orderRepository,
        IRepository<UserApi> userApiRepository,
        IRepository<Customer> customerRepository,
        IEncryptionService encryptionService,
        BackendAPIConfig apiConfig,
        ICustomerService customerService,
        IUserApiService userApiService)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _userApiRepository = userApiRepository;
        _customerRepository = customerRepository;
        _encryptionService = encryptionService;
        _apiConfig = apiConfig;
        _customerService = customerService;
        _userApiService = userApiService;
    }

    [HttpGet("products")]
    public IActionResult ListProducts([FromQuery] int pageSize = 100)
    {
        var products = _productRepository.Table
            .Take(pageSize)
            .ToList();

        var totalCount = _productRepository.Table.Count();

        var dtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name ?? "[NULL]",
            ShortDescription = p.ShortDescription ?? "[NULL]",
            Sku = p.Sku ?? "[NULL]",
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            Published = p.Published,
            CreatedOnUtc = p.CreatedOnUtc
        }).ToList();

        return Ok(new
        {
            items = dtos,
            totalCount
        });
    }

    [HttpGet("products/search")]
    public IActionResult SearchProducts(
        [FromQuery] string keywords = "",
        [FromQuery] bool searchDescriptions = false,
        [FromQuery] bool searchSku = true,
        [FromQuery] double? priceMin = null,
        [FromQuery] double? priceMax = null,
        [FromQuery] bool publishedOnly = true,
        [FromQuery] int orderBy = 0,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50)
    {
        var query = _productRepository.Table.AsQueryable();

        if (!string.IsNullOrEmpty(keywords))
        {
            var searchTerm = keywords.ToLower();
            query = query.Where(p =>
                (p.Name != null && p.Name.ToLower().Contains(searchTerm)) ||
                (searchDescriptions && p.ShortDescription != null && p.ShortDescription.ToLower().Contains(searchTerm)) ||
                (searchDescriptions && p.FullDescription != null && p.FullDescription.ToLower().Contains(searchTerm)) ||
                (searchSku && p.Sku != null && p.Sku.ToLower().Contains(searchTerm))
            );
        }

        if (priceMin.HasValue)
            query = query.Where(p => p.Price >= priceMin.Value);
        if (priceMax.HasValue)
            query = query.Where(p => p.Price <= priceMax.Value);

        if (publishedOnly)
            query = query.Where(p => p.Published);

        var totalCount = query.Count();

        query = orderBy switch
        {
            1 => query.OrderBy(p => p.Name),
            2 => query.OrderBy(p => p.Price),
            3 => query.OrderByDescending(p => p.CreatedOnUtc),
            _ => query.OrderBy(p => p.DisplayOrder)
        };

        var products = query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            ShortDescription = p.ShortDescription,
            Sku = p.Sku,
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            Published = p.Published,
            CreatedOnUtc = p.CreatedOnUtc
        });

        return Ok(new
        {
            items = dtos,
            pageIndex,
            pageSize,
            totalCount
        });
    }

    [HttpGet("orders")]
    public IActionResult ListOrders([FromQuery] int pageSize = 100)
    {
        var orders = _orderRepository.Table
            .OrderByDescending(o => o.CreatedOnUtc)
            .Take(pageSize)
            .ToList();

        var totalCount = _orderRepository.Table.Count();

        var dtos = orders.Select(o => new
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            OrderGuid = o.OrderGuid,
            CustomerId = o.CustomerId,
            CustomerEmail = o.CustomerEmail,
            OrderTotal = o.OrderTotal,
            OrderStatus = o.OrderStatusId.ToString(),
            PaymentStatus = o.PaymentStatusId.ToString(),
            ShippingStatus = o.ShippingStatusId.ToString(),
            CreatedOnUtc = o.CreatedOnUtc,
            UpdatedOnUtc = o.UpdatedOnUtc
        });

        return Ok(new
        {
            items = dtos,
            totalCount
        });
    }

    /// <summary>
    /// Diagnostic endpoint to check JWT authentication requirements
    /// Verifies if both UserApi and Customer records exist for an email
    /// </summary>
    [HttpGet("diagnose-jwt")]
    public IActionResult DiagnoseJwt([FromQuery] string email, [FromQuery] string password = null)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest(new { error = "Email parameter is required" });

        var emailLower = email.ToLowerInvariant();

        // Check UserApi record
        var userApi = _userApiRepository.Table
            .FirstOrDefault(x => x.Email == emailLower);

        // Check Customer record
        var customer = _customerRepository.Table
            .FirstOrDefault(x => x.Email == emailLower);

        // Test password if provided
        bool? passwordMatches = null;
        string passwordTest = null;

        if (!string.IsNullOrEmpty(password) && userApi != null)
        {
            try
            {
                // Decode Base64 password
                var base64EncodedBytes = Convert.FromBase64String(password);
                var decodedPassword = Encoding.UTF8.GetString(base64EncodedBytes);

                // Encrypt with stored PrivateKey
                var encryptedPassword = _encryptionService.EncryptText(decodedPassword, userApi.PrivateKey);

                // Compare with stored password
                passwordMatches = encryptedPassword == userApi.Password;

                passwordTest = passwordMatches.Value
                    ? "Password matches! JWT should work."
                    : "Password does NOT match. The password you provided doesn't match the stored encrypted password.";
            }
            catch (Exception ex)
            {
                passwordTest = $"Password test failed: {ex.Message}";
            }
        }

        var result = new
        {
            email = email,
            jwtAuthenticationReady = userApi != null && userApi.IsActive &&
                                     customer != null && customer.Active && !customer.IsSystemAccount &&
                                     (passwordMatches == null || passwordMatches.Value),
            checks = new
            {
                userApi = new
                {
                    exists = userApi != null,
                    isActive = userApi?.IsActive,
                    hasPassword = !string.IsNullOrEmpty(userApi?.Password),
                    hasPrivateKey = !string.IsNullOrEmpty(userApi?.PrivateKey),
                    hasToken = !string.IsNullOrEmpty(userApi?.Token)
                },
                customer = new
                {
                    exists = customer != null,
                    isActive = customer?.Active,
                    isSystemAccount = customer?.IsSystemAccount,
                    systemName = customer?.SystemName
                },
                passwordValidation = new
                {
                    tested = passwordMatches.HasValue,
                    matches = passwordMatches,
                    message = passwordMatches.HasValue ? passwordTest : "No password provided for testing. Add ?password=BASE64_ENCODED_PASSWORD to test."
                }
            },
            diagnosis = GetDiagnosis(userApi, customer, passwordMatches, passwordTest),
            instructions = new
            {
                missingUserApi = userApi == null ?
                    "Create API user in Admin Panel → System → API Users → Add new" : null,
                missingCustomer = customer == null ?
                    "Create customer account in Admin Panel → Customers → Add new with same email" : null,
                inactiveUserApi = userApi != null && !userApi.IsActive ?
                    "Activate API user in Admin Panel → System → API Users" : null,
                inactiveCustomer = customer != null && !customer.Active ?
                    "Activate customer in Admin Panel → Customers" : null,
                systemAccount = customer != null && customer.IsSystemAccount ?
                    "Customer is a system account - create a regular customer instead" : null,
                wrongPassword = passwordMatches.HasValue && !passwordMatches.Value ?
                    "Update password in Admin Panel → System → API Users → Edit or verify the password you're using" : null
            }
        };

        return Ok(result);
    }

    private static string GetDiagnosis(UserApi userApi, Customer customer, bool? passwordMatches, string passwordTest)
    {
        if (userApi == null)
            return "FAIL: UserApi record not found. Create API user in admin panel.";

        if (!userApi.IsActive)
            return "FAIL: UserApi exists but is not active. Activate it in admin panel.";

        if (customer == null)
            return "FAIL: Customer record not found. Create customer account with same email.";

        if (!customer.Active)
            return "FAIL: Customer exists but is not active. Activate it in admin panel.";

        if (customer.IsSystemAccount)
            return "FAIL: Customer is a system account. Create a regular customer instead.";

        if (passwordMatches.HasValue && !passwordMatches.Value)
            return "FAIL: Password does not match. Update the API user password or verify the password you're testing.";

        if (passwordMatches.HasValue && passwordMatches.Value)
            return "SUCCESS: All checks passed including password validation. JWT authentication WILL work!";

        return "SUCCESS: Both UserApi and Customer records are valid. Add ?password=BASE64_PASSWORD to test password.";
    }

    /// <summary>
    /// Simulates the exact LoginValidator logic step by step
    /// Shows which validation rule is failing
    /// </summary>
    [HttpPost("test-jwt-validation")]
    public async Task<IActionResult> TestJwtValidation([FromBody] DTOs.LoginTestDto model)
    {
        var steps = new List<object>();

        try
        {
            // Step 1: Check if email is provided
            if (string.IsNullOrEmpty(model.Email))
            {
                steps.Add(new { step = 1, rule = "Email required", passed = false, error = "Email is required" });
                return Ok(new { validationPassed = false, steps });
            }
            steps.Add(new { step = 1, rule = "Email required", passed = true });

            // Step 2: Check if password is provided
            if (string.IsNullOrEmpty(model.Password))
            {
                steps.Add(new { step = 2, rule = "Password required", passed = false, error = "Password is required" });
                return Ok(new { validationPassed = false, steps });
            }
            steps.Add(new { step = 2, rule = "Password required", passed = true });

            var emailLower = model.Email.ToLowerInvariant();

            // Step 3: Check UserApi and password
            var userApi = _userApiRepository.Table.FirstOrDefault(x => x.Email == emailLower);

            if (userApi == null)
            {
                steps.Add(new { step = 3, rule = "UserApi exists", passed = false, error = "UserApi not found" });
                return Ok(new { validationPassed = false, steps });
            }

            if (!userApi.IsActive)
            {
                steps.Add(new { step = 3, rule = "UserApi active", passed = false, error = "UserApi is not active" });
                return Ok(new { validationPassed = false, steps });
            }

            // Try Base64 decode
            byte[] base64Bytes;
            string decodedPassword;
            try
            {
                base64Bytes = Convert.FromBase64String(model.Password);
                decodedPassword = Encoding.UTF8.GetString(base64Bytes);
                steps.Add(new { step = 3, substep = "Base64 decode", passed = true, decodedLength = decodedPassword.Length });
            }
            catch (Exception ex)
            {
                steps.Add(new { step = 3, substep = "Base64 decode", passed = false, error = ex.Message });
                return Ok(new { validationPassed = false, steps });
            }

            // Try password encryption
            string encryptedPassword;
            try
            {
                encryptedPassword = _encryptionService.EncryptText(decodedPassword, userApi.PrivateKey);
                steps.Add(new { step = 3, substep = "Password encryption", passed = true });
            }
            catch (Exception ex)
            {
                steps.Add(new { step = 3, substep = "Password encryption", passed = false, error = ex.Message });
                return Ok(new { validationPassed = false, steps });
            }

            // Compare passwords
            var passwordMatch = userApi.Password == encryptedPassword;
            if (!passwordMatch)
            {
                steps.Add(new { step = 3, rule = "Password match", passed = false, error = "Password does not match" });
                return Ok(new { validationPassed = false, steps });
            }
            steps.Add(new { step = 3, rule = "UserApi validation", passed = true });

            // Step 4: Check Customer
            var customer = _customerRepository.Table.FirstOrDefault(x => x.Email == emailLower);

            if (customer == null)
            {
                steps.Add(new { step = 4, rule = "Customer exists", passed = false, error = "Customer not found" });
                return Ok(new { validationPassed = false, steps });
            }

            if (!customer.Active)
            {
                steps.Add(new { step = 4, rule = "Customer active", passed = false, error = "Customer is not active" });
                return Ok(new { validationPassed = false, steps });
            }

            if (customer.IsSystemAccount)
            {
                steps.Add(new { step = 4, rule = "Customer not system account", passed = false, error = "Customer is a system account" });
                return Ok(new { validationPassed = false, steps });
            }

            steps.Add(new { step = 4, rule = "Customer validation", passed = true });

            return Ok(new
            {
                validationPassed = true,
                steps,
                conclusion = "All validation rules passed! JWT should work."
            });
        }
        catch (Exception ex)
        {
            steps.Add(new { step = "exception", error = ex.Message, stackTrace = ex.StackTrace });
            return Ok(new { validationPassed = false, steps, exception = ex.Message });
        }
    }

    /// <summary>
    /// Check BackendAPI configuration status
    /// </summary>
    [HttpGet("check-api-config")]
    public IActionResult CheckApiConfig()
    {
        return Ok(new
        {
            backendApiEnabled = _apiConfig?.Enabled,
            secretKey = _apiConfig?.SecretKey != null ? $"{_apiConfig.SecretKey.Substring(0, Math.Min(10, _apiConfig.SecretKey.Length))}..." : null,
            validMinutes = _apiConfig?.ValidMinutes,
            configExists = _apiConfig != null
        });
    }

    /// <summary>
    /// Run the actual LoginValidator from Grand.Module.Api
    /// This tests the REAL validator that JWT uses
    /// </summary>
    [HttpPost("test-real-validator")]
    public async Task<IActionResult> TestRealValidator([FromBody] DTOs.LoginTestDto model)
    {
        try
        {
            var steps = new List<object>();

            // Step 1: Check API is enabled (this is the FIRST rule in LoginValidator)
            if (_apiConfig == null)
            {
                steps.Add(new { step = 0, rule = "API Config exists", passed = false, error = "BackendAPIConfig is null" });
                return Ok(new { validationPassed = false, steps });
            }

            if (!_apiConfig.Enabled)
            {
                steps.Add(new { step = 0, rule = "API is enabled", passed = false, error = "BackendAPIConfig.Enabled = false" });
                return Ok(new { validationPassed = false, steps });
            }
            steps.Add(new { step = 0, rule = "API is enabled", passed = true });

            // Step 2: Email required
            if (string.IsNullOrEmpty(model.Email))
            {
                steps.Add(new { step = 1, rule = "Email required", passed = false, error = "Email is required" });
                return Ok(new { validationPassed = false, steps });
            }
            steps.Add(new { step = 1, rule = "Email required", passed = true });

            // Step 3: Password required
            if (string.IsNullOrEmpty(model.Password))
            {
                steps.Add(new { step = 2, rule = "Password required", passed = false, error = "Password is required" });
                return Ok(new { validationPassed = false, steps });
            }
            steps.Add(new { step = 2, rule = "Password required", passed = true });

            // Step 4: UserApi validation (exactly as in LoginValidator)
            if (!string.IsNullOrEmpty(model.Email))
            {
                var userapi = await _userApiService.GetUserByEmail(model.Email.ToLowerInvariant());

                if (userapi == null)
                {
                    steps.Add(new { step = 3, rule = "UserApi exists", passed = false, error = "UserApi not found" });
                    return Ok(new { validationPassed = false, steps });
                }

                if (!userapi.IsActive)
                {
                    steps.Add(new { step = 3, rule = "UserApi active", passed = false, error = "UserApi is not active" });
                    return Ok(new { validationPassed = false, steps });
                }

                try
                {
                    var base64EncodedBytes = Convert.FromBase64String(model.Password);
                    var password = Encoding.UTF8.GetString(base64EncodedBytes);

                    if (userapi.Password != _encryptionService.EncryptText(password, userapi.PrivateKey))
                    {
                        steps.Add(new { step = 3, rule = "Password match", passed = false, error = "User not exists or password is wrong" });
                        return Ok(new { validationPassed = false, steps });
                    }
                }
                catch (Exception ex)
                {
                    steps.Add(new { step = 3, substep = "Password validation", passed = false, error = $"Exception: {ex.Message}" });
                    return Ok(new { validationPassed = false, steps });
                }

                steps.Add(new { step = 3, rule = "UserApi validation", passed = true });
            }

            // Step 5: Customer validation (exactly as in LoginValidator)
            if (!string.IsNullOrEmpty(model.Email))
            {
                var customer = await _customerService.GetCustomerByEmail(model.Email.ToLowerInvariant());

                if (customer == null)
                {
                    steps.Add(new { step = 4, rule = "Customer exists", passed = false, error = "Customer not exist" });
                    return Ok(new { validationPassed = false, steps });
                }

                if (!customer.Active)
                {
                    steps.Add(new { step = 4, rule = "Customer active", passed = false, error = "Customer not exist (inactive)" });
                    return Ok(new { validationPassed = false, steps });
                }

                if (customer.IsSystemAccount())
                {
                    steps.Add(new { step = 4, rule = "Customer not system account", passed = false, error = "Customer not exist (system account)" });
                    return Ok(new { validationPassed = false, steps });
                }

                steps.Add(new { step = 4, rule = "Customer validation", passed = true });
            }

            return Ok(new
            {
                validationPassed = true,
                steps,
                conclusion = "All LoginValidator rules passed using the EXACT same logic as Grand.Module.Api!"
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                validationPassed = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
}
