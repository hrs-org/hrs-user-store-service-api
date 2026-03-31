using System.Security.Claims;
using System.Text;
using FluentValidation;
using HRS.API.Filters;
using HRS.API.Middleware;
using HRS.API.Services.Helpers;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.API.Validators.Auth;
using HRS.API.Validators.User;
using HRS.Shared.Core.Authorization;
using HRS.Domain.Interfaces;
using HRS.Infrastructure;
using HRS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IAuth0ManagementService, Auth0ManagementService>();
builder.Services.AddScoped(typeof(ICrudRepository<>), typeof(CrudRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStoreRepository, StoreRepository>();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<Auth0ManagementOptions>(builder.Configuration.GetSection("Auth0Management"));

builder.Services.AddHttpClient("EmailService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["EmailEndpoint"]!);
});

builder.Services.AddHttpClient("Auth0ManagementApi", client =>
{
    var domain = builder.Configuration["Auth0Management:Domain"];
    if (!string.IsNullOrWhiteSpace(domain))
    {
        client.BaseAddress = new Uri($"https://{domain}/api/v2/");
    }
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers(options => { options.Filters.Add<ValidationFilter>(); });

builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterEmployeeDetailDtoValidators>();
builder.Services.AddValidatorsFromAssemblyContaining<ChangePasswordRequestDtoValidator>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HRS API", Version = "v1" });

    // 🔑 Enable JWT Bearer in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token.\n\nExample: **Bearer eyJhbGciOi...**"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                }
            },
            []
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString,
        ServerVersion.AutoDetect(connectionString),
        b => b.MigrationsAssembly("HRS.Migrations")));

builder.Services.AddAutoMapper(cfg => { }, typeof(Program));

var auth0Domain = builder.Configuration["Auth0:Domain"]!;
var auth0Audience = builder.Configuration["Auth0:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{auth0Domain}/";
        options.Audience = auth0Audience;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = $"https://{auth0Domain}/",
            ValidAudience = auth0Audience,
            NameClaimType = "sub"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var claims = context.Principal?.Claims.ToList() ?? new List<Claim>();
                var subClaim = claims.FirstOrDefault(c => c.Type == "sub");
                if (subClaim != null && !claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
                {
                    var claimsIdentity = (ClaimsIdentity)context.Principal?.Identity!;
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
                }
                await Task.CompletedTask;
            }
        };
    });

// Add authorization with scope-based policies
builder.Services.AddAuthorization(options =>
{
    // User management scopes
    options.AddPolicy("read:user", policy =>
        policy.Requirements.Add(new PermissionRequirement("read:user")));
    options.AddPolicy("write:user", policy =>
        policy.Requirements.Add(new PermissionRequirement("write:user")));
    options.AddPolicy("delete:user", policy =>
        policy.Requirements.Add(new PermissionRequirement("delete:user")));

    // Store management scopes
    options.AddPolicy("read:store", policy =>
        policy.Requirements.Add(new PermissionRequirement("read:store")));
    options.AddPolicy("write:store", policy =>
        policy.Requirements.Add(new PermissionRequirement("write:store")));

    // Employee management scopes
    options.AddPolicy("read:employee", policy =>
        policy.Requirements.Add(new PermissionRequirement("read:employee")));
    options.AddPolicy("update:employee", policy =>
        policy.Requirements.Add(new PermissionRequirement("update:employee")));
    options.AddPolicy("delete:employee", policy =>
        policy.Requirements.Add(new PermissionRequirement("delete:employee")));
    options.AddPolicy("write:employee", policy =>
        policy.Requirements.Add(new PermissionRequirement("write:employee")));
});

builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebClient", policy =>
        policy.WithOrigins(
                builder.Configuration["AllowedOrigins"]?.Split(',') ?? Array.Empty<string>()
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HRS API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("AllowWebClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
        logger.LogInformation("✅ Database migration applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Database migration failed.");
    }
}

app.Run();

