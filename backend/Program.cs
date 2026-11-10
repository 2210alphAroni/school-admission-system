using backend.Application.Interfaces;
using backend.Application.Services;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string is missing. " +
        "Set the ConnectionStrings__DefaultConnection environment variable."
    );
}

// Register PostgreSQL database context.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// Register application services.
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<HoldService>();

builder.Services.AddScoped<IHoldService>(
    provider => provider.GetRequiredService<HoldService>());

builder.Services.AddScoped<IHoldCreationService>(
    provider => provider.GetRequiredService<HoldService>());
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
