namespace HouseMeasurementsApi.Config;

public class ConfigOptions
{
    public required string TableStorageConnectionString { get; set; }
    
    public required string SensorName { get; set; }
    
    public required string TableName { get; set; }
}