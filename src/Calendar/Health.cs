using Calendar.Records;

namespace Calendar;

public static class Health
{
    public static HealthResponse Evaluate(params (string Name, string? Value)[] vars)
    {
        var missing = vars.Where(v => string.IsNullOrEmpty(v.Value)).Select(v => v.Name).ToArray();
        return new HealthResponse(missing.Length == 0, missing);
    }
}
