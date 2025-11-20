using Carter;
using HouseMeasurementsApi.Config;
using HouseMeasurementsApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Azure.Data.Tables;
using HouseMeasurementsApi.Helpers;

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
        
        houseMeasurementsGroup.MapGet("/getData", GetData)
            .WithName("getData")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private async Task<IResult> IngestData([FromBody] Measurement measurement)
    {
        logger.LogDebug("HouseMeasurementsEndpoints IngestData Enter");

        try
        {
            var connectionString = config.Value.TableStorageConnectionString;
            var tableName = config.Value.TableName;

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(tableName))
            {
                logger.LogWarning("HouseMeasurementsEndpoints IngestData Invalid table connection environment variables");
                return Results.BadRequest("Invalid table connection environment variables.");
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

    private async Task<GetMeasurementResponse> GetData(string? start, string? end)
    {
        logger.LogDebug("HouseMeasurementsEndpoints GetData Enter with start: {Start}, end: {End}", start, end);

        try
        {
            var connectionString = config.Value.TableStorageConnectionString;
            var tableName = config.Value.TableName;
            var sensorName = config.Value.SensorName;

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(tableName))
            {
                logger.LogWarning("HouseMeasurementsEndpoints GetData Invalid table connection environment variables");
                return new GetMeasurementResponse
                {
                    Nickname = sensorName,
                    Measurements = new List<MeasurementReading>()
                };
            }

            if (!DateTime.TryParse(start, out DateTime startDate))
            {
                logger.LogWarning("HouseMeasurementsEndpoints GetData Invalid start date: {Start}", start);
                startDate = DateTime.MinValue;
            }

            if (!DateTime.TryParse(end, out DateTime endDate))
            {
                logger.LogWarning("HouseMeasurementsEndpoints GetData Invalid end date: {End}", end);
                endDate = DateTime.MaxValue;
            }

            var startRowKey = startDate.ToUniversalTime().ToString("o");
            var endRowKey = endDate.ToUniversalTime().ToString("o");

            var client = new TableClient(connectionString, tableName);
            await client.CreateIfNotExistsAsync();
            
            var combinedFilter = TableClient.CreateQueryFilter(
                $"PartitionKey eq {sensorName} and RowKey ge {startRowKey} and RowKey le {endRowKey}"
            );

            var measurements = new List<MeasurementReading>();

            await foreach (var entity in client.QueryAsync<TableEntity>(filter: combinedFilter))
            {
                measurements.Add(new MeasurementReading
                {
                    RowKey = entity.RowKey,
                    Temperature = HelperMethods.GetNumericValue(entity, "temperature"),
                    Humidity = HelperMethods.GetNumericValue(entity,"humidity"),
                    Pressure = HelperMethods.GetNumericValue(entity,"pressure")
                });
            }


            // LTTB DOWNSAMPLING
            if (measurements.Count > 500)
            {
                logger.LogInformation(
                    "HouseMeasurementsEndpoints GetData Applying LTTB downsampling: {OriginalCount} → 500 points",
                    measurements.Count
                );

                measurements = Lttb.Downsample(measurements, 500);
            }
            else
            {
                logger.LogDebug(
                    "HouseMeasurementsEndpoints GetData Skipping downsampling, point count: {Count}",
                    measurements.Count
                );
            }

            measurements = measurements
                .OrderBy(m => m.RowKey) // Ensure sorted after downsampling
                .ToList();

            return new GetMeasurementResponse
            {
                Nickname = sensorName,
                Measurements = measurements
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HouseMeasurementsEndpoints GetData Error: {Message}", ex.Message);
            return new GetMeasurementResponse
            {
                Nickname = config.Value.SensorName,
                Measurements = new List<MeasurementReading>()
            };
        }
        finally
        {
            logger.LogDebug("HouseMeasurementsEndpoints GetData Leave");
        }
    }
}