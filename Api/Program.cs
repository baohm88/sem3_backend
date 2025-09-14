using System.Text;
using Api.Common;
using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

using System.Reflection;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var conn = builder.Configuration.GetConnectionString("mysql") ?? "server=localhost;uid=root;pwd=Bao@123;database=mvp_platform";
    opt.UseMySql(conn, ServerVersion.AutoDetect(conn));
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// --- SwaggerGen ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MVP API",
        Version = "v1",
        Description = "Company — Driver — Rider"
    });

    // Show [SwaggerOperation]/[SwaggerTag] metadata in UI
    c.EnableAnnotations();

    // Load XML documentation from this assembly (requires GenerateDocumentationFile=true)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

    // Stable schema ids & deterministic endpoint ordering
    c.CustomSchemaIds(t => t.FullName);
    c.OrderActionsBy(d => $"{d.GroupName}_{d.RelativePath}_{d.HttpMethod}");
    c.SupportNonNullableReferenceTypes();

    // ====== BEARER JWT (Authorize button) ======
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the **JWT only** (no need to type 'Bearer '). Click Authorize and paste the token."
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { jwtScheme, Array.Empty<string>() }
    });
});

// Auth (JWT)
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-secret-key-change-me";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ClockSkew = TimeSpan.Zero
    };
});

// CORS: allow any origin/headers/methods (relaxed; tighten for production)
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.DocumentTitle = "MVP API";
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "MVP API v1");

    // Compact list view, enable filter & request duration, hide Schemas panel
    o.DocExpansion(DocExpansion.List);
    o.EnableFilter();
    o.DisplayRequestDuration();
    o.DefaultModelsExpandDepth(-1);

    // (optional) keep default look & feel; if your custom CSS breaks layout, remove InjectStylesheet.
    o.InjectStylesheet("/swagger-ui/custom.css");

    // Enable "Try it out" by default (optional)
    o.ConfigObject.AdditionalItems["tryItOutEnabled"] = true;
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
