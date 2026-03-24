using System.Text;
using ArtemisBanking.Application.Interfaces.Services;
using ArtemisBanking.Infrastructure;
using ArtemisBanking.Infrastructure.Seeds;
using ArtemisBanking.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ArtemisBanking.WebAPI.Filters;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IJwtService, JwtService>();

var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
    opt.Events = new JwtBearerEvents
    {
        OnChallenge = ctx =>
        {
            ctx.HandleResponse();
            ctx.Response.StatusCode = 401;
            ctx.Response.ContentType = "application/json";
            return ctx.Response.WriteAsync("{\"message\":\"No tiene autorización.\"}");
        },
        OnForbidden = ctx =>
        {
            ctx.Response.StatusCode = 403;
            ctx.Response.ContentType = "application/json";
            return ctx.Response.WriteAsync("{\"message\":\"Acceso denegado.\"}");
        }
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Artemis Banking API",
        Version = "v1",
        Description = "API REST del sistema bancario Artemis Banking.\n\n" +
            "## Autenticación\n" +
            "La mayoría de endpoints requieren Bearer JWT. Para autenticarte:\n" +
            "1. Ejecuta **POST /api/account/login** con tus credenciales.\n" +
            "2. Copia el `jwt` de la respuesta.\n" +
            "3. Haz clic en **Authorize** e ingresa: `Bearer <tu_token>`.",
        Contact = new OpenApiContact { Name = "Dev1 - Albertson (Backend · Auth · API)" }
    });

    // Esquema de seguridad Bearer con descripción
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Bearer token. Ejemplo: Bearer eyJhbGci...",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    // Aplica JWT globalmente
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme { Reference = new OpenApiReference
            { Type = ReferenceType.SecurityScheme, Id = "Bearer" }},
        Array.Empty<string>()
    }});

    // Tags agrupados por módulo
    c.TagActionsBy(api =>
    {
        if (api.GroupName != null) return new[] { api.GroupName };
        if (api.ActionDescriptor.RouteValues.TryGetValue("controller", out var ctrl))
            return new[] { ctrl! };
        return new[] { "Other" };
    });
    c.OrderActionsBy(api => $"{api.GroupName}_{api.HttpMethod}_{api.RelativePath}");

    // Marcar endpoints públicos
    c.OperationFilter<SwaggerPublicEndpointFilter>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Artemis Banking API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Artemis Banking API";
        c.DefaultModelsExpandDepth(-1);
        c.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

await DefaultUserSeeder.SeedAsync(app.Services);

app.MapControllers();
app.Run();