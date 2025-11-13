namespace HouseMeasurementsApi.Models;

public class GetMeasurementResponse
{
    public required string Nickname { get; set; }
    public required List<MeasurementReading> Measurements { get; set; } = [];
}

public class MeasurementReading
{
    public required string RowKey { get; set; } // ISO timestamp from Azure Table
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }
}