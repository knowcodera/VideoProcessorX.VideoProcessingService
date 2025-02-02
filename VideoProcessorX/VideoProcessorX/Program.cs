using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using VideoProcessingService.Application.Interfaces;
using VideoProcessingService.Application.Services;
using VideoProcessingService.Domain.Interfaces;
using VideoProcessingService.Domain.Services;
using VideoProcessingService.Infrastructure.Data;
using VideoProcessingService.Infrastructure.Messaging;
using VideoProcessingService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RabbitMqListener>();
builder.Services.AddHostedService<RabbitMqHostedService>();

builder.Services.AddSingleton<IMessageQueue>(sp =>
    new RabbitMqMessageQueue("localhost"));

builder.Services.AddScoped<IVideoRepository, VideoRepository>();
builder.Services.AddScoped<IVideoProcessor, VideoProcessor>();
builder.Services.AddScoped<IVideoService, VideoService>();


// Adicionar no ConfigureServices
builder.Services.AddHostedService<VideoProcessingWorker>();

// Adicionar configuração do RabbitMQ
//builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero

        };
    });

builder.Services.AddControllers();

builder.Services.Configure<FormOptions>(options =>
{
    // Exemplo: 500 MB (em bytes)
    options.MultipartBodyLengthLimit = 500L * 1024L * 1024L;
});

builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "VideoProcessorX API",
        Version = "v1"
    });

    // 1) Definir como o Swagger interpretará o esquema de segurança
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Ex: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // 2) Exigir que todas as operações estejam protegidas por esse esquema
    c.AddSecurityRequirement(new OpenApiSecurityRequirement{
    {
        new OpenApiSecurityScheme {
            Reference = new OpenApiReference {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }});

});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VideoProcessorX v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
