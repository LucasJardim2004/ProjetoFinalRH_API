
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RhManagementApi.Data;
using RhManagementApi.Models;
using RhManagementApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// AdventureWorks context
builder.Services.AddDbContext<AdventureWorksContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AdventureWorks"), sql => sql.UseHierarchyId()));

// Identity context (auth schema with separate migrations history)
builder.Services.AddDbContext<AuthDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("AdventureWorks"), sql =>
        sql.MigrationsHistoryTable("__EFMigrationsHistory", "auth")));

builder.Services.AddScoped<TokenService>();

// Swagger + JWT security
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: **Bearer {your JWT}**"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Identity Core (int keys) — adjust to your custom types User/Role
builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddRoles<Role>()                              // int-keyed role type
    .AddEntityFrameworkStores<AuthDbContext>()     // int-keyed context
    .AddSignInManager();

// JWT Bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})

.AddJwtBearer(options =>
{
    var issuer = builder.Configuration["Jwt:Issuer"];
    var audience = builder.Configuration["Jwt:Audience"];
    var key = builder.Configuration["Jwt:Key"];

    if (string.IsNullOrWhiteSpace(key))
        throw new InvalidOperationException("Jwt:Key is missing. Set it in appsettings or environment variables.");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuerSigningKey = true,
        ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
        ValidateAudience = !string.IsNullOrWhiteSpace(audience),
        ClockSkew = TimeSpan.Zero
    };

});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));

    options.AddPolicy("CanHireEmployee", p =>
        p.RequireRole("RH", "Admin")
         .RequireClaim("business_entity_id"));
});

// (Optional) CORS for React SPA — keep if you call from a different origin
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
        // .AllowCredentials(); // only if you use cookies
    });
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// 🔹 Swagger ALWAYS ON
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.DocumentTitle = "RH Management API Docs";
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
    c.DefaultModelExpandDepth(2);
    c.DefaultModelsExpandDepth(-1); // hide schemas by default

    // Optional: serve Swagger UI at root "/"
    // c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();

// (Optional) CORS for SPA
app.UseCors("Frontend");

//// 🔹 Correct order: Authentication → Authorization → MapControllers
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();