
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RhManagementApi.Models;

namespace RhManagementApi.Services
{
    public class JwtOptions
    {
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public string Key { get; set; } = default!; // 32+ bytes recommended
        public int AccessTokenMinutes { get; set; } = 60;  // default 60 minutes
        public int RefreshTokenDays { get; set; } = 14;    // default 14 days
    }

    public class TokenService
    {
        private readonly UserManager<User> _userManager;
        private readonly JwtOptions _options;

        public TokenService(UserManager<User> userManager, IOptions<JwtOptions> options)
        {
            _userManager = userManager;
            _options = options.Value;
        }

        public async Task<string> CreateAccessTokenAsync(User user)
        {
            // Guard: ensure options are present
            if (string.IsNullOrWhiteSpace(_options.Key))
                throw new InvalidOperationException("JWT Key is not configured.");
            if (string.IsNullOrWhiteSpace(_options.Issuer))
                throw new InvalidOperationException("JWT Issuer is not configured.");
            if (string.IsNullOrWhiteSpace(_options.Audience))
                throw new InvalidOperationException("JWT Audience is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim("full_name", user.FullName ?? string.Empty),
                new Claim("business_entity_id", user.BusinessEntityID.ToString())
            };

            // Add role claims
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var now = DateTime.UtcNow;
            var jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(_options.AccessTokenMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public (string token, DateTime expires) CreateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64); 
            var token = Convert.ToBase64String(bytes);

            var expires = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);
            return (token, expires);
        }
    }
}
