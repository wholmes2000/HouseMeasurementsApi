using Carter;
using HouseMeasurementsApi.Config;
using HouseMeasurementsApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Azure.Data.Tables;

namespace HouseMeasurementsApi.Endpoints;

public class HouseMeasurementsEndpoints(ILogger<HouseMeasurementsEndpoints> logger, IOptions<ConfigOptions> config) : ICarterModule
{

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var houseMeasurementsGroup = app.MapGroup("/api")
            .WithName("HouseMeasurements");

        houseMeasurementsGroup.MapPost("/ingestData", IngestData)
            .WithName("ingestData")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private async Task<IResult> IngestData([FromBody] Measurement measurement)
    {
        logger.LogDebug("HouseMeasurementsEndpoints IngestData Enter");

        try
        {
            var connectionString = config.Value.MyTableStorageConnectionString;
            var tableName = config.Value.MyTableName;

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(tableName))
            {
                logger.LogWarning("HouseMeasurementsEndpoints IngestData Invalid table connection environment variables");
                return Results.BadRequest("Invalid table connection environment variables.");
            }

            if (measurement is null)
            {
                logger.LogWarning("HouseMeasurementsEndpoints IngestData Request body is null");
                return Results.BadRequest("Missing request body.");
            }

            if (string.IsNullOrWhiteSpace(measurement.Uid) ||
                measurement.Readings.Temperature == 0 ||
                measurement.Readings.Humidity == 0 ||
                measurement.Readings.Pressure == 0)
            {
                logger.LogWarning("HouseMeasurementsEndpoints IngestData Missing or invalid fields in measurement: {@Measurement}", measurement);
                return Results.BadRequest("Missing or invalid uid or readings with temperature, humidity, or pressure.");
            }

            var partitionKey = !string.IsNullOrWhiteSpace(measurement.Nickname)
                ? measurement.Nickname.Trim()
                : "sensor1";

            if (!DateTime.TryParse(measurement.Timestamp, out DateTime parsedDate))
            {
                parsedDate = DateTime.UtcNow;
            }

            var rowKey = parsedDate.ToUniversalTime().ToString("o");

            var entity = new TableEntity(partitionKey, rowKey)
            {
                { "temperature", measurement.Readings.Temperature },
                { "humidity", measurement.Readings.Humidity },
                { "pressure", measurement.Readings.Pressure },
                { "uid", measurement.Uid }
            };

            var client = new TableClient(connectionString, tableName);
            await client.CreateIfNotExistsAsync();
            await client.AddEntityAsync(entity);

            logger.LogInformation("HouseMeasurementsEndpoints IngestData Data stored successfully: {Entity}", entity);
            return Results.Ok(new { message = "Data stored", rowKey });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HouseMeasurementsEndpoints IngestData Error: {Message}", ex.Message);
            return TypedResults.Problem( $"Failed to store data. Error: {ex.Message}", statusCode: StatusCodes.Status500InternalServerError);
        }
        finally
        {
            logger.LogDebug("HouseMeasurementsEndpoints IngestData Leave");
        }
    }
}