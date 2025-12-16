
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RhManagementApi.Data;
using RhManagementApi.Models;
using RhManagementApi.Services;

// Adjust the namespace to your project
namespace YourApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly SignInManager<User> _signInManager;
        private readonly TokenService _tokenService;
        private readonly AuthDbContext _authDb;
        private readonly AdventureWorksContext _aw; // Optional: only needed if you validate BusinessEntityID against AW

        public AuthController(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            SignInManager<User> signInManager,
            TokenService tokenService,
            AuthDbContext authDb,
            AdventureWorksContext aw) // remove if not validating against AW
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _authDb = authDb;
            _aw = aw;
        }

        // ==========================
        // DTOs
        // ==========================
        public record RegisterDto(string Email, string UserName, string Password, string FullName, int BusinessEntityID);
        public record LoginDto(string Email, string Password);
        public record TokenResponse(string AccessToken, string RefreshToken);
        public record RefreshRequest(string RefreshToken);

        // ==========================
        // REGISTER
        // ==========================
        /// <summary>
        /// Registers a new user. Assigns the default "Employee" role.
        /// Returns access & refresh tokens.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // Optional: validate BusinessEntityID exists in AdventureWorks Employee table
            if (dto.BusinessEntityID is int beId)
            {
                var exists = await _aw.Employees.AnyAsync(e => e.BusinessEntityID == beId);
                if (!exists)
                    return BadRequest("BusinessEntityID is not an AdventureWorks Employee.");
            }

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                FullName = dto.FullName,
                BusinessEntityID = dto.BusinessEntityID,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            // Assign a default role (Employee). You can change based on your workflow.
            await EnsureRoleAsync("Employee");
            await _userManager.AddToRoleAsync(user, "Employee");

            // Issue tokens
            var accessToken = await _tokenService.CreateAccessTokenAsync(user);
            var (refreshToken, refreshExpires) = _tokenService.CreateRefreshToken();
            await SaveRefreshTokenAsync(user.Id, refreshToken, refreshExpires);

            return Ok(new TokenResponse(accessToken, refreshToken));
        }

        // ==========================
        // LOGIN
        // ==========================
        /// <summary>
        /// Logs in with email/password. Returns access & refresh tokens.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user is null) return Unauthorized("Invalid credentials");

            var pw = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
            if (!pw.Succeeded) return Unauthorized("Invalid credentials");

            var accessToken = await _tokenService.CreateAccessTokenAsync(user);

            var (refreshToken, refreshExpires) = _tokenService.CreateRefreshToken();
            await SaveRefreshTokenAsync(user.Id, refreshToken, refreshExpires);

            return Ok(new TokenResponse(accessToken, refreshToken));
        }

        // ==========================
        // REFRESH
        // ==========================
        /// <summary>
        /// Exchanges a valid (non-revoked) refresh token for a new access & refresh token (rotation).
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
        {
            var entry = await _authDb.Set<UserRefreshToken>()
                .FirstOrDefaultAsync(x => x.Token == req.RefreshToken && !x.Revoked && x.Expires > DateTime.UtcNow);

            if (entry is null) return Unauthorized("Invalid refresh token");

            var user = await _userManager.FindByIdAsync(entry.UserId.ToString());
            if (user is null) return Unauthorized();

            // Rotate refresh token
            entry.Revoked = true;

            var (newRefresh, expires) = _tokenService.CreateRefreshToken();
            await SaveRefreshTokenAsync(user.Id, newRefresh, expires);

            var access = await _tokenService.CreateAccessTokenAsync(user);
            await _authDb.SaveChangesAsync();

            return Ok(new TokenResponse(access, newRefresh));
        }

        // ==========================
        // LOGOUT
        // ==========================
        /// <summary>
        /// Revokes the provided refresh token (server-side logout).
        /// </summary>
        [HttpPost("logout")]
        [Authorize] // optional: require auth to logout
        public async Task<IActionResult> Logout([FromBody] RefreshRequest req)
        {
            var entry = await _authDb.Set<UserRefreshToken>().FirstOrDefaultAsync(x => x.Token == req.RefreshToken);
            if (entry != null)
            {
                entry.Revoked = true;
                await _authDb.SaveChangesAsync();
            }
            return Ok();
        }

        // ==========================
        // ME
        // ==========================
        /// <summary>
        /// Returns the current user's basic info as present in the JWT.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var uniqueName = User.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? User.Identity?.Name;
            var fullName = User.FindFirst("full_name")?.Value;
            var beId = User.FindFirst("business_entity_id")?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray();

            return Ok(new
            {
                sub,
                userName = uniqueName,
                fullName,
                businessEntityID = beId,
                roles
            });
        }

        // ==========================
        // Helpers
        // ==========================

        private async Task EnsureRoleAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new Role { Name = roleName });
            }
        }


        private async Task SaveRefreshTokenAsync(int userId, string token, DateTime expires)
        {
            _authDb.Set<UserRefreshToken>().Add(new UserRefreshToken
            {
                UserId = userId,
                Token = token,
                Expires = expires,
                Revoked = false
            });
            await _authDb.SaveChangesAsync();
        }
    }
}
