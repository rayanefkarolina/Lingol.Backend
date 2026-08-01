using Lingol.Cadastro.Infrastructure.Persistence;
using Lingol.Domain.Entities;
using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var cadastroConnectionString = configuration.GetConnectionString("Cadastro")
    ?? throw new InvalidOperationException("Connection string 'Cadastro' não configurada.");

var jwtKey = configuration["Jwt:Key"] ?? "ChaveSuperSecretaLingol123!";
var jwtIssuer = configuration["Jwt:Issuer"] ?? "Lingol.Auth";
var jwtAudience = configuration["Jwt:Audience"] ?? "Lingol.Client";

builder.Services.AddDbContext<CadastroDbContext>(options =>
{
    options.UseSqlServer(cadastroConnectionString);
});

builder.Services.AddControllers();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Lingol Cadastro API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpClient<ICadastroClient, CadastroHttpClient>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri(configuration["CadastroService:BaseUrl"] ?? "http://localhost:5000/");
        client.Timeout = TimeSpan.FromSeconds(10);
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lingol Cadastro API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();