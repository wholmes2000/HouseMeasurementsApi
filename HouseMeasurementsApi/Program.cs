using Carter;
using HouseMeasurementsApi.Config;

var builder = WebApplication.CreateBuilder(args);

// Load configuration (from appsettings.json, environment, etc.)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true);

builder.Configuration.AddEnvironmentVariables();

// Bind ConfigOptions from configuration
builder.Services.Configure<ConfigOptions>(
    builder.Configuration.GetSection("Config"));

// Add Carter and Swagger
builder.Services.AddCarter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000", "https://thankful-ocean-0d7207e03.3.azurestaticapps.net") // React dev server and Deployed React server
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();

// Use CORS
app.UseCors("AllowLocalhost");

// Development-only OpenAPI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "House Measurements API V1");
    });
    app.MapGet("/", async context =>
    {
        context.Response.Redirect("/swagger");
        await Task.CompletedTask;
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapCarter();

app.Run();