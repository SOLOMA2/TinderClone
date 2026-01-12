using StackExchange.Redis;
using TinderClone.MatchingService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Redis Configuration
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    return ConnectionMultiplexer.Connect(redisConnectionString);
});

// Register services
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
builder.Services.AddScoped<IMatchingService, MatchingService>();

// RabbitMQ Consumer (Background Service)
builder.Services.AddHostedService<RabbitMqMessageConsumer>();

// HTTP Client for main API
builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
{
    var mainApiUrl = builder.Configuration["MainApi:BaseUrl"] ?? "https://localhost:7000";
    client.BaseAddress = new Uri(mainApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Для MatchingService нужен отдельный HttpClient для вызова основного API
builder.Services.AddHttpClient<MatchingService>(client =>
{
    var mainApiUrl = builder.Configuration["MainApi:BaseUrl"] ?? "https://localhost:7000";
    client.BaseAddress = new Uri(mainApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

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

app.Run();
