using System.Text;
using System.Threading.Channels;
using Lingol.Pedagogico.API.Http;
using Lingol.Pedagogico.API.Hubs;
using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Application.Commands;
using Lingol.Pedagogico.Application.Queries;
using Lingol.Pedagogico.Application.Queue;
using Lingol.Pedagogico.Infrastructure.Persistence;
using Lingol.Pedagogico.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ----------------------------------------------------------------------
// 1. Configurações base (connection string, JWT, CORS)
// ----------------------------------------------------------------------

var pedagogicoConnectionString = configuration.GetConnectionString("Pedagogico")
    ?? throw new InvalidOperationException("Connection string 'Pedagogico' não configurada.");

var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key não configurada.");
var jwtIssuer = configuration["Jwt:Issuer"] ?? "Lingol.Auth";
var jwtAudience = configuration["Jwt:Audience"] ?? "Lingol.Client";

var origensPermitidas = configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

// ----------------------------------------------------------------------
// 2. EF Core
// ----------------------------------------------------------------------

builder.Services.AddDbContext<PedagogicoDbContext>(options =>
{
    options.UseSqlServer(pedagogicoConnectionString);
});

builder.Services.AddScoped<IPedagogicoDbContext>(provider =>
    provider.GetRequiredService<PedagogicoDbContext>());

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// ----------------------------------------------------------------------
// 3. MediatR
// ----------------------------------------------------------------------

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CriarAtividadeCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(ObterRelatorioDificuldadesTurmaQuery).Assembly);
});

// ----------------------------------------------------------------------
// 4. Provedor de IA e cliente do microsserviço de Cadastro
// ----------------------------------------------------------------------

builder.Services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SecaoConfig));

// IaProvider:UseFake = true usa o gerador simulado, util para testar o fluxo
// (202 -> fila -> SignalR) sem consumir cota da API.
if (configuration.GetValue<bool>("IaProvider:UseFake"))
{
    builder.Services.AddScoped<IIaAtividadeService, FakeIaAtividadeService>();
}
else
{
    var geminiOptions = configuration.GetSection(GeminiOptions.SecaoConfig).Get<GeminiOptions>()
        ?? new GeminiOptions();

    if (string.IsNullOrWhiteSpace(geminiOptions.ApiKey))
    {
        throw new InvalidOperationException(
            "Gemini:ApiKey nao configurada. Rode: dotnet user-secrets set \"Gemini:ApiKey\" \"<token do AI Studio>\" " +
            "ou defina IaProvider:UseFake = true para rodar com questoes simuladas.");
    }

    builder.Services.AddHttpClient<IIaAtividadeService, GeminiAtividadeService>(client =>
    {
        client.BaseAddress = new Uri(geminiOptions.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(geminiOptions.TimeoutSegundos);
    });
}

builder.Services.AddTransient<AuthHeaderPropagationHandler>();

builder.Services.AddHttpClient<ICadastroClient, CadastroHttpClient>(client =>
{
    client.BaseAddress = new Uri(configuration["CadastroService:BaseUrl"] ?? "http://localhost:5030/");
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddHttpMessageHandler<AuthHeaderPropagationHandler>();

// ----------------------------------------------------------------------
// 5. Authentication + Authorization (JWT)
// ----------------------------------------------------------------------

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // O SignalR envia o token via query string (access_token) no handshake.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AlunoPolicy", policy => policy.RequireRole("Aluno"));
    options.AddPolicy("ProfessorPolicy", policy => policy.RequireRole("Professor"));
});

// ----------------------------------------------------------------------
// 6. CORS (Angular em http://localhost:4200)
// ----------------------------------------------------------------------

builder.Services.AddCors(options =>
{
    options.AddPolicy("LingolFrontend", policy =>
        policy.WithOrigins(origensPermitidas)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // necessário para o SignalR
});

// ----------------------------------------------------------------------
// 7. Fila in-process (System.Threading.Channels) + BackgroundService
// ----------------------------------------------------------------------

var atividadeChannel = Channel.CreateUnbounded<GerarAtividadeQueueItem>();
builder.Services.AddSingleton(atividadeChannel.Reader);
builder.Services.AddSingleton(atividadeChannel.Writer);

var correcaoChannel = Channel.CreateUnbounded<CorrigirEntregaQueueItem>();
builder.Services.AddSingleton(correcaoChannel.Reader);
builder.Services.AddSingleton(correcaoChannel.Writer);

builder.Services.AddHostedService<GerarAtividadeBackgroundService>();
builder.Services.AddHostedService<CorrigirEntregaBackgroundService>();

// ----------------------------------------------------------------------
// 8. SignalR + notificador
// ----------------------------------------------------------------------

builder.Services.AddSignalR();
builder.Services.AddSingleton<IAtividadeNotifier, Lingol.Pedagogico.API.Services.SignalRAtividadeNotifier>();

// ----------------------------------------------------------------------
// 9. Swagger / OpenAPI
// ----------------------------------------------------------------------

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Lingol Pedagógico API",
        Version = "v1",
        Description = "Microsserviço pedagógico: atividades, respostas, correção por IA e relatórios."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Bearer token. Exemplo: \"Bearer {token}\"",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = "Bearer",
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

// ----------------------------------------------------------------------
// 10. Pipeline HTTP
// ----------------------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lingol Pedagógico API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseRouting();

app.UseCors("LingolFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Lingol.Pedagogico.API" }))
   .AllowAnonymous();

app.MapHub<AtividadeHub>("/hubs/atividades");
app.MapControllers();

app.Run();
