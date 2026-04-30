using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.Json;
using WalletSystem.Core.Interfaces.Repositories;
using WalletSystem.Core.Interfaces.Services;
using WalletSystem.Infrastructure.Config;
using WalletSystem.Infrastructure.Data;
using WalletSystem.Infrastructure.ExternalServices;
using WalletSystem.Infrastructure.Repositories;
using WalletSystem.Services.Auth;
using WalletSystem.Services.Background;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<WalletContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddOptions<SmtpSettings>()
    .BindConfiguration("SmtpSettings")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<JwtSettings>().BindConfiguration("JwtSettings")
    .ValidateDataAnnotations().Validate(s =>
    {
        return File.Exists(s.PrivateKeyPath) &&
        File.Exists(s.PublicKeyPath);
    }, "JWt key files are missing").ValidateOnStart();


builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserCredentialsRepository , UserCredentialRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVpaRepository, VpaRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<WalletContext>());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
        );
    });

builder.Services.AddHostedService<KycAutoVerificationService>();


var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
var rsa = RSA.Create();
if (!File.Exists(jwtSettings.PublicKeyPath))
{
    throw new FileNotFoundException("Key file missing", jwtSettings.PublicKeyPath);
}
var pemContent = File.ReadAllText(jwtSettings.PublicKeyPath);
rsa.ImportFromPem(pemContent);

builder.Services.AddSingleton(rsa);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Configure the HTTP request pipeline.

app.Run();