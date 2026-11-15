using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Judoo.Application;
using Judoo.Application.Common.Interfaces;
using Judoo.Domain.Entities;
using Judoo.Domain.Enums;
using Judoo.Domain.Repositories;
using Judoo.Infrastructure;
using Judoo.Infrastructure.Data;
using Judoo.Infrastructure.Hubs;
using Judoo.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar serviços de Domínio, Aplicação e Infraestrutura (Clean Architecture com EF Core + Dapper)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Autenticação JWT Bearer e Autorização por Perfis (Admin, Secretaria, Mesario, Publico)
var jwtKey = builder.Configuration["Jwt:Key"] ?? JwtDefaults.DefaultKey;
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? JwtDefaults.DefaultIssuer;
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? JwtDefaults.DefaultAudience;

if (builder.Environment.IsProduction() && jwtKey == JwtDefaults.DefaultKey)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("[AVISO DE SEGURANÇA] Ambiente de Produção detectado usando a chave JWT padrão. Defina a variável de ambiente Jwt__Key.");
    Console.ResetColor();
}

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });

builder.Services.AddAuthorization();

// 3. Controladores REST com conversão de Enums para String
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 4. Swagger / OpenAPI configurável por ambiente (Ativo em Dev e Homologação/Staging)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = $"Judoo API - [{builder.Environment.EnvironmentName}] Torneios de Judô em Tempo Real",
        Version = "v1",
        Description = $"API de Alta Performance ({builder.Environment.EnvironmentName}) com Clean Architecture, CQRS, Dapper, Entity Framework Core (Migrations), PostgreSQL, JWT RBAC e SignalR para Gestão de Torneios de Judô FIJ."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT no formato: Bearer {seu_token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// 5. CORS configurado de acordo com o ambiente (Dev permissivo, Homologação/Produção estritos)
var allowAnyOrigin = builder.Configuration.GetValue<bool>("Cors:AllowAnyOrigin", builder.Environment.IsDevelopment());
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("JudooCorsPolicy", policy =>
    {
        if (allowAnyOrigin || allowedOrigins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// 6. Aplicar Migrations do EF Core e seed inicial de usuários padrão (respeitando ambiente)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<JudooDbContext>();
    try
    {
        logger.LogInformation("Verificando e aplicando Entity Framework Core Migrations pendentes no ambiente {Environment}...", app.Environment.EnvironmentName);
        dbContext.Database.Migrate();
        dbContext.Database.ExecuteSqlRaw(@"
            ALTER TABLE placar_lutas ADD COLUMN IF NOT EXISTS yuko_branco INT NOT NULL DEFAULT 0;
            ALTER TABLE placar_lutas ADD COLUMN IF NOT EXISTS yuko_azul INT NOT NULL DEFAULT 0;
        ");
        logger.LogInformation("Migrations aplicadas com sucesso no PostgreSQL.");
    }
    catch (Exception ex)
    {
        logger.LogWarning("Aviso ao conectar no banco para aplicar migrations automáticas: {Message}", ex.Message);
    }

    var seedDemoUsers = app.Configuration.GetValue<bool>("Security:SeedDemoUsers", !app.Environment.IsProduction());
    if (seedDemoUsers)
    {
        try
        {
            var usuarioRepo = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var usuariosPadrao = new[]
            {
                new Usuario { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Nome = "Coordenador Geral (Admin)", Email = "admin@judoo.com", SenhaHash = hasher.Hash("123456"), Role = PerfilUsuario.Admin, Ativo = true },
                new Usuario { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Nome = "Secretaria Oficial de Pesagem", Email = "secretaria@judoo.com", SenhaHash = hasher.Hash("123456"), Role = PerfilUsuario.Secretaria, Ativo = true },
                new Usuario { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Nome = "Mesário Oficial do Tatame", Email = "mesario1@judoo.com", SenhaHash = hasher.Hash("123456"), Role = PerfilUsuario.Mesario, Ativo = true },
                new Usuario { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), Nome = "Espectador / Público", Email = "publico@judoo.com", SenhaHash = hasher.Hash("123456"), Role = PerfilUsuario.Publico, Ativo = true }
            };

            foreach (var u in usuariosPadrao)
            {
                var existente = await usuarioRepo.ObterPorEmailAsync(u.Email);
                if (existente == null)
                {
                    await usuarioRepo.CriarAsync(u);
                }
                else if (string.IsNullOrWhiteSpace(existente.SenhaHash) || !existente.SenhaHash.StartsWith("pbkdf2$"))
                {
                    existente.SenhaHash = u.SenhaHash;
                    await usuarioRepo.AtualizarAsync(existente);
                }
            }
            logger.LogInformation("Usuários padrão verificados/semeados para o ambiente {Environment}.", app.Environment.EnvironmentName);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Aviso ao semear usuários padrão: {Message}", ex.Message);
        }
    }
}

// 7. Pipeline HTTP e Swagger
var swaggerEnabled = app.Configuration.GetValue<bool>("Swagger:Enabled", app.Environment.IsDevelopment() || app.Environment.IsStaging());
if (swaggerEnabled)
{
    var routePrefix = app.Configuration.GetValue<string>("Swagger:RoutePrefix", app.Environment.IsDevelopment() ? string.Empty : "swagger");
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", $"Judoo API v1 ({app.Environment.EnvironmentName})");
        c.RoutePrefix = routePrefix;
    });
}

app.UseCors("JudooCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

// 8. Roteamento de Controllers e SignalR Hub
app.MapControllers();
app.MapHub<TatameHub>("/hubs/tatame");

// 9. Health Check & Database Migration Endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow,
    engine = ".NET 8 Judoo Engine (EF Core Migrations + Dapper Queries)",
}));

app.MapPost("/api/database/migrate", (JudooDbContext db) =>
{
    db.Database.Migrate();
    return Results.Ok(new { mensagem = "Migrations do Entity Framework Core aplicadas com sucesso!" });
}).RequireAuthorization(p => p.RequireRole("Admin")).WithTags("Database");

app.Run();

public partial class Program { }
