using LeaseApi.Options;
using LeaseApi.Services;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Threading.RateLimiting;
using WitnessBackendEngineerTask.Common.Options;
using WitnessBackendEngineerTask.Common.Resilience;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));
builder.Services.Configure<RedisRetryOptions>(builder.Configuration.GetSection(RedisRetryOptions.SectionName));
builder.Services.Configure<ParserOptions>(builder.Configuration.GetSection(ParserOptions.SectionName));
builder.Services.AddControllers();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("lease-get", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Lease API",
        Version = "v1",
        Description = "Lease lookup API with asynchronous parsing and Redis-backed status/result cache."
    });

    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var options = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
    return ConnectionMultiplexer.Connect(options.ConnectionString);
});
builder.Services.AddSingleton<ILeaseCache, RedisLeaseCache>();
builder.Services.AddSingleton<IRetryRunner, RetryRunner>();
builder.Services.AddHttpClient<IParseTrigger, FunctionsParseTrigger>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.MapGet("/", () => "LeaseApi is running.");
app.MapControllers();

app.Run();
