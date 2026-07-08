using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RoadmapPlatform.Application.Interfaces.Auth;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Infrastructure.Services.Auth;

/// <summary>
/// Generates JWT access tokens for authenticated users.
/// </summary>
/// <remarks>
/// This service is the infrastructure implementation of IJwtTokenService.
/// It creates signed JWT tokens using issuer, audience, expiration, and signing key
/// values from JwtSettings.
/// </remarks>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;

    /// <summary>
    /// Creates a new JWT token service.
    /// </summary>
    /// <param name="jwtOptions">
    /// The configured JWT settings loaded from application configuration.
    /// </param>
    public JwtTokenService(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    /// <summary>
    /// Generates a signed JWT access token for an authenticated user.
    /// </summary>
    /// <param name="userId">
    /// The unique identifier of the authenticated user.
    /// </param>
    /// <param name="username">
    /// The username of the authenticated user.
    /// </param>
    /// <param name="roles">
    /// The roles assigned to the authenticated user.
    /// </param>
    /// <returns>
    /// A signed JWT access token string.
    /// </returns>
    /// <remarks>
    /// The generated token includes:
    /// - The user ID as NameIdentifier.
    /// - The username as Name.
    /// - One Role claim for each assigned role.
    ///
    /// The token is signed with HMAC SHA-256 using the configured JWT key.
    /// </remarks>
    public string GenerateToken(Guid userId, string username, IEnumerable<string> roles)
    {
        // Add the basic identity claims used by the API authentication pipeline.
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username ?? string.Empty)
        };
        
        // Add one role claim for each role assigned to the user.
        foreach(var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Create the symmetric signing key from the configured JWT secret.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));

        // Use HMAC SHA-256 to sign the token.
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Create the JWT token with issuer, audience, claims, expiration, and signature.
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials);

        // Serialize the token object into a compact JWT string.
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
