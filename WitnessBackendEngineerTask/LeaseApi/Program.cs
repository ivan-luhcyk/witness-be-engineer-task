using LeaseApi.Clients;
using LeaseApi.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HmlrOptions>(builder.Configuration.GetSection(HmlrOptions.SectionName));
builder.Services.AddHttpClient<IHmlrClient, HmlrClient>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "LeaseApi is running.");
app.MapControllers();

app.Run();
