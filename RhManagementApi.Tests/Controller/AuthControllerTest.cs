
using Microsoft.Extensions.Options;
using Xunit;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RhManagementApi.Api.Controllers;
using RhManagementApi.Data;
using RhManagementApi.DTOs;
using RhManagementApi.Models;
using RhManagementApi.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public class AuthControllerTests
{
    // ============================================================
    // REAL TokenService (Required: TokenService cannot be faked)
    // ============================================================
    private static TokenService RealTokenService(UserManager<User> um)
    {
        var jwt = Options.Create(new JwtOptions
        {
            Key = "THIS_IS_A_32+_BYTE_TEST_KEY_FOR_JWT_1234567890",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenMinutes = 60,
            RefreshTokenDays = 7
        });

        return new TokenService(um, jwt);
    }

    // ============================================================
    // Fake Managers
    // ============================================================
    private static RoleManager<Role> FakeRoleManager()
    {
        return A.Fake<RoleManager<Role>>(x =>
            x.WithArgumentsForConstructor(() => new RoleManager<Role>(
                A.Fake<IRoleStore<Role>>(),
                null, null, null, null
            )));
    }

    private static SignInManager<User> FakeSignInManager(UserManager<User> um)
    {
        return A.Fake<SignInManager<User>>(x =>
            x.WithArgumentsForConstructor(() => new SignInManager<User>(
                um,
                A.Fake<IHttpContextAccessor>(),
                A.Fake<IUserClaimsPrincipalFactory<User>>(),
                null, null, null, null
            )));
    }

    // ============================================================
    // Real EF-backed UserManager (Needed ONLY for Login tests)
    // ============================================================
    private static (AuthDbContext authDb, UserManager<User> userManager) RealUserManager()
    {
        var opts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var authDb = new AuthDbContext(opts);

        var store = new UserStore<User, Role, AuthDbContext, int>(authDb);
        var userManager = new UserManager<User>(
            store,
            null,
            new PasswordHasher<User>(),
            new IUserValidator<User>[0],
            new IPasswordValidator<User>[0],
            null, null, null, null
        );

        return (authDb, userManager);
    }

    // ============================================================
    // BuildController: Injects real TokenService
    // ============================================================
    private AuthController BuildController(
        UserManager<User>? um = null,
        RoleManager<Role>? rm = null,
        SignInManager<User>? sm = null,
        TokenService? ts = null,
        AuthDbContext? authDb = null,
        AdventureWorksContext? aw = null)
    {
        authDb ??= new AuthDbContext(
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        aw ??= new AdventureWorksContext(
            new DbContextOptionsBuilder<AdventureWorksContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        um ??= A.Fake<UserManager<User>>(x =>
            x.WithArgumentsForConstructor(() => new UserManager<User>(
                A.Fake<IUserStore<User>>(),
                null, null, null, null, null, null, null, null)));

        rm ??= FakeRoleManager();
        sm ??= FakeSignInManager(um);

        ts ??= RealTokenService(um);

        return new AuthController(um, rm, sm, ts, authDb, aw);
    }

    // ============================================================
    // REGISTER TESTS
    // ============================================================
    [Fact]
    public async Task Register_ReturnsOk()
    {
        var aw = new AdventureWorksContext(
            new DbContextOptionsBuilder<AdventureWorksContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        
        aw.Employees.Add(new Employee 
        {
            BusinessEntityID = 1,
            NationalIDNumber = "123456789",
            JobTitle = "Developer",
            BirthDate = DateTime.UtcNow.AddYears(-30),
            MaritalStatus = "S",
            Gender = "M",
            HireDate = DateTime.UtcNow.AddYears(-1),
            SalariedFlag = true,
        });
        await aw.SaveChangesAsync();


        var um = A.Fake<UserManager<User>>(x =>
            x.WithArgumentsForConstructor(() => new UserManager<User>(
                A.Fake<IUserStore<User>>(), null, null, null, null, null, null, null, null)));

        A.CallTo(() => um.CreateAsync(A<User>._, A<string>._))
            .Returns(IdentityResult.Success);

        var controller = BuildController(um: um, aw: aw);

        var dto = new RegisterDTO
        {
            UserName = "john",
            Email = "john@test.com",
            FullName = "John Doe",
            Password = "Pass123!",
            BusinessEntityID = 1
        };

        var result = await controller.Register(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<TokenResponseDTO>(ok.Value);
    }

    [Fact]
    public async Task Register_BadRequest_WhenEmployeeNotFound()
    {
        var controller = BuildController();

        var dto = new RegisterDTO
        {
            UserName = "x",
            Email = "y",
            Password = "a",
            BusinessEntityID = 999
        };

        var result = await controller.Register(dto);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("BusinessEntityID is not an AdventureWorks Employee.", bad.Value);
    }

    // ============================================================
    // LOGIN TESTS
    // ============================================================
    [Fact]
    public async Task Login_ReturnsOk()
    {
        var (authDb, realUm) = RealUserManager();
        var user = new User { Id = 1, Email = "test@test.com", UserName = "test" };

        authDb.Users.Add(user);
        await authDb.SaveChangesAsync();

        var sm = FakeSignInManager(realUm);

        A.CallTo(() => sm.CheckPasswordSignInAsync(user, "123", true))
            .Returns(global::Microsoft.AspNetCore.Identity.SignInResult.Success);

        var ts = RealTokenService(realUm);

        var controller = BuildController(um: realUm, sm: sm, ts: ts, authDb: authDb);

        var dto = new LoginDTO { Email = "test@test.com", Password = "123" };

        var result = await controller.Login(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<TokenResponseDTO>(ok.Value);
    }

    [Fact]
    public async Task Login_Unauthorized_WhenInvalidCredentials()
    {
        var (authDb, realUm) = RealUserManager();

        var controller = BuildController(um: realUm, authDb: authDb);

        var dto = new LoginDTO { Email = "wrong@test.com", Password = "bad" };

        var result = await controller.Login(dto);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ============================================================
    // UPDATE ROLES TESTS
    // ============================================================
    [Fact]
    public async Task UpdateRoles_ReturnsOk()
    {
        var um = A.Fake<UserManager<User>>(x =>
            x.WithArgumentsForConstructor(() => new UserManager<User>(
                A.Fake<IUserStore<User>>(), null, null, null, null, null, null, null, null)));

        var rm = FakeRoleManager();
        var ts = RealTokenService(um);

        var user = new User { Id = 10 };

        A.CallTo(() => um.FindByIdAsync("10")).Returns(user);
        A.CallTo(() => um.IsInRoleAsync(user, "HR")).Returns(false);
        A.CallTo(() => um.AddToRoleAsync(user, "HR")).Returns(IdentityResult.Success);
        A.CallTo(() => rm.RoleExistsAsync("HR")).Returns(true);
        A.CallTo(() => um.GetRolesAsync(user))
            .Returns(Task.FromResult<IList<string>>(new List<string> { "Employee", "HR" }));

        var controller = BuildController(um: um, rm: rm, ts: ts);

        var dto = new UpdateRoleDTO
        {
            UserId = 10,
            AddRoles = new List<string> { "HR" }
        };

        var result = await controller.UpdateRoles(dto);

        var ok = Assert.IsType<OkObjectResult>(result);

        var val = ok.Value!;
        var rolesProp = val.GetType().GetProperty("roles");
        var roles = (IList<string>)rolesProp!.GetValue(val)!;

        Assert.Contains("HR", roles);
    }

    [Fact]
    public async Task UpdateRoles_NotFound_WhenUserMissing()
    {
        var um = A.Fake<UserManager<User>>(x =>
            x.WithArgumentsForConstructor(() => new UserManager<User>(
                A.Fake<IUserStore<User>>(), null, null, null, null, null, null, null, null)));

        A.CallTo(() => um.FindByIdAsync("999")).Returns(Task.FromResult<User?>(null));

        var controller = BuildController(um: um);

        var result = await controller.UpdateRoles(new UpdateRoleDTO { UserId = 999 });

        var nf = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User 999 not found.", nf.Value);
    }

    // ============================================================
    // REFRESH TOKEN TESTS
    // ============================================================
    [Fact]
    public async Task Refresh_ReturnsOk()
    {
        var authDb = new AuthDbContext(
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        authDb.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId = 10,
            Token = "old123",
            Expires = DateTime.UtcNow.AddMinutes(5)
        });

        await authDb.SaveChangesAsync();

        var um = A.Fake<UserManager<User>>(x =>
            x.WithArgumentsForConstructor(() => new UserManager<User>(
                A.Fake<IUserStore<User>>(), null, null, null, null, null, null, null, null)));

        var user = new User { Id = 10 };
        A.CallTo(() => um.FindByIdAsync("10")).Returns(user);

        var ts = RealTokenService(um);

        var controller = BuildController(um: um, ts: ts, authDb: authDb);

        var req = new RefreshRequestDTO("old123");

        var result = await controller.Refresh(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<TokenResponseDTO>(ok.Value);
    }

    [Fact]
    public async Task Refresh_Unauthorized_WhenInvalidToken()
    {
        var controller = BuildController();

        var result = await controller.Refresh(new RefreshRequestDTO("invalid"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ============================================================
    // LOGOUT TESTS
    // ============================================================
    [Fact]
    public async Task Logout_ReturnsOk()
    {
        var authDb = new AuthDbContext(
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        authDb.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId = 1,
            Token = "abc",
            Expires = DateTime.UtcNow.AddMinutes(5)
        });

        await authDb.SaveChangesAsync();

        var controller = BuildController(authDb: authDb);

        var result = await controller.Logout(new RefreshRequestDTO("abc"));

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Logout_Ok_WhenTokenMissing()
    {
        var controller = BuildController();

        var result = await controller.Logout(new RefreshRequestDTO("doesnotexist"));

        Assert.IsType<OkResult>(result);
    }

    // ============================================================
    // ME TESTS
    // ============================================================
    [Fact]
    public void Me_ReturnsUserInfo()
    {
        var controller = BuildController();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim("full_name", "John Doe"),
            new Claim("business_entity_id", "10"),
            new Claim(ClaimTypes.Role, "Employee")
        }));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var result = controller.Me();

        var ok = Assert.IsType<OkObjectResult>(result);

        var val = ok.Value!;
        Assert.Equal("John Doe", val.GetType().GetProperty("fullName")!.GetValue(val));
        Assert.Equal("10", val.GetType().GetProperty("business_entityID")?.GetValue(val) ?? 
                           val.GetType().GetProperty("businessEntityID")!.GetValue(val));

        var roles = (string[])val.GetType().GetProperty("roles")!.GetValue(val)!;
        Assert.Contains("Employee", roles);
    }

    [Fact]
    public void Me_ReturnsNulls_WhenClaimsMissing()
    {
        var controller = BuildController();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = controller.Me();

        var ok = Assert.IsType<OkObjectResult>(result);

        var val = ok.Value!;
        Assert.Null(val.GetType().GetProperty("fullName")!.GetValue(val));
    }
}
