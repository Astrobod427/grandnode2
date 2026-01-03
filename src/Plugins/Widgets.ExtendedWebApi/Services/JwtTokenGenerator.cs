using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Widgets.ExtendedWebApi.Services;

public class JwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(Dictionary<string, string> claims)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var secretKey = _configuration["BackendAPI:SecretKey"] ?? "GrandNodeSecretKeyForJWT2024";
        var key = Encoding.ASCII.GetBytes(secretKey);

        var claimsList = new List<Claim>();
        foreach (var claim in claims)
        {
            claimsList.Add(new Claim(claim.Key, claim.Value));
        }

        var expiryMinutes = int.Parse(_configuration["BackendAPI:ExpiryInMinutes"] ?? "60");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claimsList),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var validateIssuer = bool.Parse(_configuration["BackendAPI:ValidateIssuer"] ?? "false");
        if (validateIssuer)
        {
            var issuer = _configuration["BackendAPI:ValidIssuer"];
            if (!string.IsNullOrEmpty(issuer))
            {
                tokenDescriptor.Issuer = issuer;
            }
        }

        var validateAudience = bool.Parse(_configuration["BackendAPI:ValidateAudience"] ?? "false");
        if (validateAudience)
        {
            var audience = _configuration["BackendAPI:ValidAudience"];
            if (!string.IsNullOrEmpty(audience))
            {
                tokenDescriptor.Audience = audience;
            }
        }

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
