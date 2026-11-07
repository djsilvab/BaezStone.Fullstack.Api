using BaezStone.Fullstack.Api.Data;
using BaezStone.Fullstack.Api.Dtos;
using BaezStone.Fullstack.Api.Middleware;
using BaezStone.Fullstack.Api.Repositorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ClienteConnection")));

builder.Services.AddAutoMapper(typeof(Program).Assembly);

builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();

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
builder.Services.AddSwaggerGen();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
