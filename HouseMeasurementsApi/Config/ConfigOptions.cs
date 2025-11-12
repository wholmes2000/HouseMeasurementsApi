namespace HouseMeasurementsApi.Config;

public class ConfigOptions
{
    public required string MyTableStorageConnectionString { get; set; }
    
    public required string MySensorName { get; set; }
    
    public required string MyTableName { get; set; }
}