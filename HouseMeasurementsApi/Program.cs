using Carter;
using HouseMeasurementsApi.Config;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Load configuration (from appsettings.json, environment, etc.)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Bind ConfigOptions from configuration
builder.Services.Configure<ConfigOptions>(
    builder.Configuration.GetSection("Config"));

// Add Carter and Swagger
builder.Services.AddCarter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Development-only OpenAPI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "House Measurements API V1");
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapCarter();

// Example: show configuration values in a simple endpoint
app.MapGet("/config", (IOptions<ConfigOptions> config) =>
{
    var c = config.Value;
    return Results.Ok(new
    {
        Table = c.MyTableName,
        Sensor = c.MySensorName,
        ConnectionStringSet = !string.IsNullOrEmpty(c.MyTableStorageConnectionString)
    });
});

app.Run();