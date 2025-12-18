
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RhManagementApi.Data;
using RhManagementApi.DTOs;
using RhManagementApi.Models;
using RhManagementApi.Services;

namespace RhManagementApi.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly SignInManager<User> _signInManager;
        private readonly TokenService _tokenService;
        private readonly AuthDbContext _authDb;
        private readonly AdventureWorksContext _aw; 

        public AuthController(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            SignInManager<User> signInManager,
            TokenService tokenService,
            AuthDbContext authDb,
            AdventureWorksContext aw) 
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _authDb = authDb;
            _aw = aw;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
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

            await EnsureRoleAsync("Employee");
            await _userManager.AddToRoleAsync(user, "Employee");

            var accessToken = await _tokenService.CreateAccessTokenAsync(user);
            var (refreshToken, refreshExpires) = _tokenService.CreateRefreshToken();
            await SaveRefreshTokenAsync(user.Id, refreshToken, refreshExpires);

            return Ok(new TokenResponseDTO(accessToken, refreshToken));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user is null) return Unauthorized("Invalid credentials");

            var pw = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
            if (!pw.Succeeded) return Unauthorized("Invalid credentials");

            var accessToken = await _tokenService.CreateAccessTokenAsync(user);

            var (refreshToken, refreshExpires) = _tokenService.CreateRefreshToken();
            await SaveRefreshTokenAsync(user.Id, refreshToken, refreshExpires);

            return Ok(new TokenResponseDTO(accessToken, refreshToken));
        }

        [HttpPost("update-roles")]
        public async Task<IActionResult> UpdateRoles([FromBody] UpdateRoleDTO dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
            if (user == null) return NotFound($"User {dto.UserId} not found.");

            if (dto.AddRoles != null && dto.AddRoles.Any())
            {
                foreach (var role in dto.AddRoles)
                {
                    await EnsureRoleAsync(role);
                    if (!await _userManager.IsInRoleAsync(user, role))
                        await _userManager.AddToRoleAsync(user, role);
                }
            }

            if (dto.RemoveRoles != null && dto.RemoveRoles.Any())
            {
                foreach (var role in dto.RemoveRoles)
                {
                    if (await _userManager.IsInRoleAsync(user, role))
                        await _userManager.RemoveFromRoleAsync(user, role);
                }
            }

            var updatedRoles = await _userManager.GetRolesAsync(user);
            var newAccessToken = await _tokenService.CreateAccessTokenAsync(user);

            return Ok(new
            {
                userId = user.Id,
                roles = updatedRoles,
                accessToken = newAccessToken
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDTO req)
        {
            var entry = await _authDb.Set<UserRefreshToken>()
                .FirstOrDefaultAsync(x => x.Token == req.RefreshToken && !x.Revoked && x.Expires > DateTime.UtcNow);

            if (entry is null) return Unauthorized("Invalid refresh token");

            var user = await _userManager.FindByIdAsync(entry.UserId.ToString());
            if (user is null) return Unauthorized();

            entry.Revoked = true;

            var (newRefresh, expires) = _tokenService.CreateRefreshToken();
            await SaveRefreshTokenAsync(user.Id, newRefresh, expires);

            var access = await _tokenService.CreateAccessTokenAsync(user);
            await _authDb.SaveChangesAsync();

            return Ok(new TokenResponseDTO(access, newRefresh));
        }

        [HttpPost("logout")]
        [Authorize] // optional: require auth to logout
        public async Task<IActionResult> Logout([FromBody] RefreshRequestDTO req)
        {
            var entry = await _authDb.Set<UserRefreshToken>().FirstOrDefaultAsync(x => x.Token == req.RefreshToken);
            if (entry != null)
            {
                entry.Revoked = true;
                await _authDb.SaveChangesAsync();
            }
            return Ok();
        }

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
