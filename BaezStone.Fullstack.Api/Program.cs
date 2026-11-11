using BaezStone.Fullstack.Api.Data;
using BaezStone.Fullstack.Api.Dtos;
using BaezStone.Fullstack.Api.Middleware;
using BaezStone.Fullstack.Api.Repositorio;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ClienteConnection")));

builder.Services.AddAutoMapper(typeof(Program).Assembly);

builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,            
            ValidIssuer = builder.Configuration.GetSection("AppSettings:Issuer").Value 
                            ?? throw new InvalidOperationException("AppSettings:Issuer no está configurado."),
            ValidAudience = builder.Configuration.GetSection("AppSettings:Audience").Value
                            ?? throw new InvalidOperationException("AppSettings:Audience no está configurado."),
            ClockSkew = TimeSpan.FromMinutes(
                            int.TryParse(builder.Configuration.GetSection("AppSettings:ClockSkewMinutes").Value, out int clockSkewMinutes) ? clockSkewMinutes : 5),
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value ?? throw new InvalidOperationException("JWT key no está configurado.")))
        };
    });

builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<SecurityRequirementsOperationFilter>();
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BaezStone.Fullstack.Api",
        Version = "v1"
    });
});

builder.Services.AddCors(options => 
    options.AddPolicy("Angular", policy => 
        policy.WithOrigins("https://localhost:4200", "http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()));

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                                .Where(e => e.Value?.Errors.Count > 0)
                                .SelectMany(x => x.Value!.Errors)
                                .Select(x => x.ErrorMessage)
                                .ToList();
        
            return new BadRequestObjectResult(new ResponseDto
            {
                IsSuccess = false,
                DisplayMessage = "Error de validación",
                ErrorMessages = errors
            });
        };
    });


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Global error handling middleware should be registered early in the pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Angular");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
