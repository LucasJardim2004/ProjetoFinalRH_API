using Microsoft.EntityFrameworkCore;
using RhManagementApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();

// AdventureWorks context
builder.Services.AddDbContext<AdventureWorksContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AdventureWorks"), sql => sql.UseHierarchyId()));

// // Identity context
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("AdventureWorks"), sql => sql.UseHierarchyId()));

// builder.Services
//     .AddIdentity<User, IdentityRole>(options => 
//     {
//         options.User.RequireUniqueEmail = true;
//         options.Password.RequiredLength = 10;
//         options.Password.RequiredDigit = true;
//         options.Password.RequireUpperCase = true;
//         options.Password.RequireNonAlphanumeric = true;
//     })
//     .AddEntityFrameworkStores<AppDbContext>()
//     .AddDefaultTokenProviders();

// var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
// var jwtAudience = builder.Configuration["Jwt:Audience"]!;
// var jwtSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!));

// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateIssuer = true, ValidIssuer = jwtIssuer,
//             ValidateAudience = true, ValidateAudience = jwtAudience,
//             ValidateIssuerSigningKey = true, IssuerSigningKey = jwtSigningKey,
//             ValidateLifetime = true,
//             NameClaimType = ClaimTypes.Name,
//             RoleClaimType = ClaimTypes.Role
//         };
//     });

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();