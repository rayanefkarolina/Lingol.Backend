using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Application.Commands;
using Lingol.Pedagogico.Application.Queries;
using Lingol.Pedagogico.Infrastructure.Persistence;
using Lingol.Pedagogico.Infrastructure.Services;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.OpenApi;
using System.Security.Claims;
using System.Text;
using Lingol.Pedagogico.Infrastructure.Persistence;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
// using Microsoft.OpenApi; // not required

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------
// 1. Configurações base (connection string, JWT)
// ----------------------------------------------------------------------

var configuration = builder.Configuration;

// Connection string do banco pedagógico (ajuste para seu SQL Server)
var pedagogicoConnectionString = configuration.GetConnectionString("Pedagogico") ??
    "Server=localhost,1433;Database=LingolPedagogico;User Id=sa;Password=SqlServer@123;TrustServerCertificate=True;";

// Config JWT compartilhado com o Cadastro.API
var jwtKey = configuration["Jwt:Key"] ?? "ChaveSuperSecretaLingol123!";
var jwtIssuer = configuration["Jwt:Issuer"] ?? "Lingol.Auth";
var jwtAudience = configuration["Jwt:Audience"] ?? "Lingol.Client";

// ----------------------------------------------------------------------
// 2. EF Core – DbContext do Pedagógico
// ----------------------------------------------------------------------

builder.Services.AddDbContext<PedagogicoDbContext>(options =>
{
    options.UseSqlServer(pedagogicoConnectionString);
});

// Controllers
builder.Services.AddControllers();

// Registrar a interface do DbContext para que handlers que dependem de
// IPedagogicoDbContext possam ser resolvidos pelo container de DI.
builder.Services.AddScoped<IPedagogicoDbContext>(provider =>
    provider.GetRequiredService<PedagogicoDbContext>());

// ----------------------------------------------------------------------
// 3. MediatR – Commands/Queries da camada Application
// ----------------------------------------------------------------------

// Ajuste os tipos abaixo conforme o namespace real dos seus handlers
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CriarAtividadeCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(ObterRelatorioDificuldadesTurmaQuery).Assembly);
});

// ----------------------------------------------------------------------
// 4. HttpClient para IA e Cadastro
// ----------------------------------------------------------------------

// IIaAtividadeService -> serviço de IA (implementado em Lingol.Pedagogico.Infrastructure)
builder.Services.AddHttpClient<IIaAtividadeService, IaHttpClient>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri(configuration["IaProvider:BaseUrl"] ?? "http://localhost:5010/");
        client.Timeout = TimeSpan.FromSeconds(60);
    });

// ICadastroClient -> microsserviço de Cadastro
builder.Services.AddHttpClient<ICadastroClient, CadastroHttpClient>(client =>
{
    client.BaseAddress = new Uri(configuration["CadastroService:BaseUrl"] ?? "http://localhost:5000/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

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
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AlunoPolicy", policy =>
        policy.RequireClaim("role", "Aluno"));

    options.AddPolicy("ProfessorPolicy", policy =>
        policy.RequireClaim("role", "Professor"));
});

// ----------------------------------------------------------------------
// 6. MassTransit - opcional: registrar apenas quando habilitado em configuração
// ----------------------------------------------------------------------

// Para evitar erro de licença/requirement ao executar localmente, o MassTransit
// será registrado somente se a chave MassTransit:Enabled estiver true.
// Habilite em appsettings ou via variável de ambiente quando necessário.
if (configuration.GetValue<bool>("MassTransit:Enabled"))
{
    builder.Services.AddMassTransit(x =>
    {
        // Em Development podemos optar por InMemory; em outros ambientes RabbitMQ
        if (builder.Environment.IsDevelopment())
        {
            x.UsingInMemory((context, cfg) => { });
        }
        else
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitHost = configuration["RabbitMQ:Host"] ?? "rabbitmq";
                var rabbitUser = configuration["RabbitMQ:User"] ?? "admin";
                var rabbitPass = configuration["RabbitMQ:Pass"] ?? "admin123";

                cfg.Host(rabbitHost, "/", h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });
            });
        }
    });
}

// ----------------------------------------------------------------------
// 7. Swagger / OpenAPI
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

    // Configuração de segurança para JWT no Swagger
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

// Constrói a aplicação após registrar todos os serviços
var app = builder.Build();


// ----------------------------------------------------------------------
// 8. Pipeline HTTP
// ----------------------------------------------------------------------

// Habilita página de exceção detalhada em desenvolvimento para depuração de erros
app.UseDeveloperExceptionPage();

// Habilita Swagger/UI (temporariamente fora do bloco de desenvolvimento para diagnóstico)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lingol Pedagógico API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ----------------------------------------------------------------------
// 9. Endpoints básicos (health) – para testar se API está up
// ----------------------------------------------------------------------

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Lingol.Pedagogico.API" }))
   .AllowAnonymous()
   .WithOpenApi(operation =>
   {
       operation.Summary = "Health check da API pedagógica.";
       operation.Description = "Retorna status simples para verificar se o serviço está em execução.";
       return operation;
   });
// Mapeia controllers (endpoints via atributos [ApiController])
app.MapControllers();
// Fim da configuração de endpoints. O app será executado abaixo.

app.Run();
