
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using RhManagementApi.Models;

namespace RhManagementApi.Services
{
    /// <summary>
    /// Responsible for issuing JWT access tokens and generating secure refresh tokens.
    /// </summary>
    public class TokenService
    {
        private readonly IConfiguration _config;
        private readonly UserManager<User> _userManager;

        public TokenService(IConfiguration config, UserManager<User> userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        /// <summary>
        /// Creates a signed JWT access token for the specified user, embedding roles and custom claims.
        /// </summary>
        public async Task<string> CreateAccessTokenAsync(User user)
        {
            // Read JWT settings
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            var keyBytes = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
            var signingKey = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            // Roles
            var roles = await _userManager.GetRolesAsync(user);

            // Claims (include your custom fields)

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("full_name", user.FullName ?? ""),
                new Claim("business_entity_id", user.BusinessEntityID?.ToString() ?? "")
            };


            // Optional but useful claims
            if (!string.IsNullOrWhiteSpace(user.Email))
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

            // Role claims
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            // Lifetime
            var minutes = int.TryParse(_config["Jwt:AccessTokenMinutes"], out var m) ? m : 15;

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Generates a cryptographically strong refresh token and its expiration timestamp.
        /// </summary>
        public (string Token, DateTime Expires) CreateRefreshToken()
        {
            // 64 bytes of entropy → Base64 (~88 chars). Adjust length if you prefer shorter.
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            var refreshToken = Convert.ToBase64String(bytes);

            var days = int.TryParse(_config["Jwt:RefreshTokenDays"], out var d) ? d : 7;
            var expires = DateTime.UtcNow.AddDays(days);

            return (refreshToken, expires);
        }

        /// <summary>
        /// (Optional) Validates a JWT without checking its expiration, useful for refresh flows
        /// if you need to read claims from an expired access token.
        /// </summary>
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudience = _config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)),
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false // <-- allow expired tokens
            };

            var handler = new JwtSecurityTokenHandler();
            try
            {
                var principal = handler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                if (securityToken is JwtSecurityToken jwt &&
                    jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return principal;
                }
            }
            catch
            {
                // swallow and return                // swallow and return null
            }
            return null;
        }
    }
}