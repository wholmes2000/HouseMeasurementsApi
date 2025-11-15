using Azure.Data.Tables;

namespace HouseMeasurementsApi.Helpers;

public class HelperMethods
{
    public static double GetNumericValue(TableEntity entity, string key)
    {
        if (!entity.ContainsKey(key))
            return 0;

        var value = entity[key];

        return value switch
        {
            double d => d,
            int i => (double)i,
            long l => (double)l,
            float f => (double)f,
            decimal m => (double)m,
            _ => 0
        };
    }
}