using Grand.Infrastructure.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Widgets.ExtendedWebApi.Services;

public class JwtTokenGenerator
{
    private readonly BackendAPIConfig _apiConfig;

    public JwtTokenGenerator(BackendAPIConfig apiConfig)
    {
        _apiConfig = apiConfig;
    }

    public string GenerateToken(Dictionary<string, string> claims)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_apiConfig.SecretKey);

        var claimsList = new List<Claim>();
        foreach (var claim in claims)
        {
            claimsList.Add(new Claim(claim.Key, claim.Value));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claimsList),
            Expires = DateTime.UtcNow.AddMinutes(_apiConfig.ExpiryInMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        if (_apiConfig.ValidateIssuer && !string.IsNullOrEmpty(_apiConfig.ValidIssuer))
        {
            tokenDescriptor.Issuer = _apiConfig.ValidIssuer;
        }

        if (_apiConfig.ValidateAudience && !string.IsNullOrEmpty(_apiConfig.ValidAudience))
        {
            tokenDescriptor.Audience = _apiConfig.ValidAudience;
        }

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
