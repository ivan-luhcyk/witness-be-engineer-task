using LeaseProcessing.Functions.Options;
using LeaseProcessing.Functions.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using WitnessBackendEngineerTask.Common.Options;
using WitnessBackendEngineerTask.Common.Resilience;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.Configure<HmlrOptions>(builder.Configuration.GetSection(HmlrOptions.SectionName));
builder.Services.Configure<HmlrRetryOptions>(builder.Configuration.GetSection(HmlrRetryOptions.SectionName));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));
builder.Services.Configure<RedisRetryOptions>(builder.Configuration.GetSection(RedisRetryOptions.SectionName));
builder.Services.Configure<ParserAuthOptions>(builder.Configuration.GetSection(ParserAuthOptions.SectionName));

builder.Services.AddHttpClient<IHmlrClient, HmlrClient>();
builder.Services.AddSingleton<IScheduleParser, ScheduleParser>();
builder.Services.AddSingleton<ILeaseStore, RedisLeaseStore>();
builder.Services.AddSingleton<IRetryRunner, RetryRunner>();
builder.Services.AddSingleton<IParseRequestAuthenticator, HmacParseRequestAuthenticator>();
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var redis = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
    return ConnectionMultiplexer.Connect(redis.ConnectionString);
});

builder.Build().Run();
