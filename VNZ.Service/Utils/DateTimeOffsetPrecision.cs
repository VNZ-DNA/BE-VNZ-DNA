namespace VNZ.Service.Utils;

/// <summary>
/// PostgreSQL timestamp-with-time-zone values have microsecond precision.
/// Concurrency tokens must be truncated before the entity is saved so the
/// value returned in the mutation response is the same value read back by EF.
/// </summary>
public static class DateTimeOffsetPrecision
{
    public static DateTimeOffset UtcNowMicrosecond()
    {
        return TruncateToMicroseconds(DateTimeOffset.UtcNow);
    }

    public static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        var ticks = utc.Ticks - utc.Ticks % TimeSpan.TicksPerMicrosecond;
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
