using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TinderClone.Domain.Interfaces;
using TinderClone.Infrastructure.Cache;
using TinderClone.Infrastructure.ExternalServices;
using TinderClone.Infrastructure.MessageQueue;
using TinderClone.Infrastructure.Persistence;
using TinderClone.Infrastructure.Persistence.Repositories;
using TinderClone.Application.Services;
using Amazon.S3;
using Amazon;
using Amazon.Extensions.NETCore.Setup;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// PostgreSQL with PostGIS
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.UseNetTopologySuite(); // PostGIS support
    });
});

// Redis Configuration
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    return ConnectionMultiplexer.Connect(redisConnectionString);
});

// AWS S3 Configuration
var s3Config = builder.Configuration.GetSection("S3");
if (!string.IsNullOrEmpty(s3Config["AccessKey"]) && !string.IsNullOrEmpty(s3Config["SecretKey"]))
{
    builder.Services.AddAWSService<IAmazonS3>(new AWSOptions
    {
        Region = RegionEndpoint.GetBySystemName(s3Config["Region"] ?? "us-east-1"),
        Credentials = new Amazon.Runtime.BasicAWSCredentials(
            s3Config["AccessKey"],
            s3Config["SecretKey"])
    });
}
else
{
    // Для локальной разработки можно использовать LocalStack или MinIO
    builder.Services.AddAWSService<IAmazonS3>();
}

// Register services
// Repositories
builder.Services.AddScoped<IUnitOfWork, AppDbContext>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISwipeRepository, SwipeRepository>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();

// Cache
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

// File Storage
builder.Services.AddScoped<IFileStorageService, S3FileStorageService>();

// Message Queue (RabbitMQ)
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

// Application Services
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// Configure the HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created (for development)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        dbContext.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating the database");
    }
}

app.Run();
