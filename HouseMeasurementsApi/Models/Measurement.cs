namespace HouseMeasurementsApi.Models;

public class Measurement
{
    public required string Nickname { get; set; }
    
    public required string Uid { get;  set; }
    
    public required string Timestamp { get; set; }
    
    public required Reading Readings { get; set; }
    
    public required string Model { get; set; }
}

public class Reading
{
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }
}