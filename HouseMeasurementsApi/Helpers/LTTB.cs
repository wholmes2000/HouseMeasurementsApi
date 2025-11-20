using HouseMeasurementsApi.Models;

namespace HouseMeasurementsApi.Helpers;

public static class Lttb
{
    public static List<MeasurementReading> Downsample(List<MeasurementReading> data, int threshold)
    {
        try
        {
            if (data.Count <= threshold)
                return data;

            var sampled = new List<MeasurementReading>(threshold);

            double rawBucketSize = (double)(data.Count - 2) / (threshold - 2);
            int bucketSize = (int)Math.Ceiling(rawBucketSize);

            sampled.Add(data[0]); // Always include first

            int nextSampleIndex = 0;

            for (int i = 0; i < threshold - 2; i++)
            {
                int rangeStart = 1 + i * bucketSize;
                int rangeEnd = Math.Min(1 + (i + 1) * bucketSize, data.Count - 1);

                var pointA = sampled[^1]; // use last added sample
                double maxArea = -1;
                MeasurementReading? selected = null;

                for (int j = rangeStart; j < rangeEnd; j++)
                {
                    var pointB = data[j];
                    double area = Math.Abs(
                        (pointA.DateTicks() - pointB.DateTicks()) *
                        (data[nextSampleIndex].ValueAvg() - pointB.ValueAvg())
                        - (pointA.ValueAvg() - pointB.ValueAvg()) *
                        (data[nextSampleIndex].DateTicks() - pointB.DateTicks())
                    ) / 2.0;

                    if (area > maxArea)
                    {
                        maxArea = area;
                        selected = pointB;
                        nextSampleIndex = j;
                    }
                }

                if (selected != null)
                    sampled.Add(selected);
            }

            sampled.Add(data[^1]); // Always include last

            return sampled;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }
}

public static class LttbExtensions
{
    // Helper properties to avoid repeated parsing
    public static long DateTicks(this MeasurementReading m)
        => DateTime.Parse(m.RowKey).Ticks;

    public static double ValueAvg(this MeasurementReading m)
        => (m.Temperature + m.Humidity + m.Pressure) / 3.0;
}