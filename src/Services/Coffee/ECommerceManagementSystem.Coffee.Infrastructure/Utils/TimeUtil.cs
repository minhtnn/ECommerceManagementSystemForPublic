namespace ECommerceManagementSystem.Coffee.Infrastructure.Utils;

public class TimeUtil
{
    public static DateTime GetCurrentSEATime()
    {
        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        DateTime localTime = DateTime.Now;
        DateTime utcTime = TimeZoneInfo.ConvertTime(localTime, TimeZoneInfo.Local, tz);
        return utcTime;
    }
    
    /// <summary>
    /// Convert DateTime từ timezone chỉ định sang UTC.
    /// DateTime truyền vào phải là Unspecified hoặc Local (giờ địa phương của timezone đó).
    /// </summary>
    public static DateTime? ConvertToUtc(DateTime? localDateTime, string? ianaTimeZone)
    {
        if (localDateTime == null) return null;
        if (string.IsNullOrWhiteSpace(ianaTimeZone)) return localDateTime; // Giả sử đã là UTC

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZone);

            var unspecified = DateTime.SpecifyKind(localDateTime.Value, DateTimeKind.Unspecified);

            return TimeZoneInfo.ConvertTimeToUtc(unspecified, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"Timezone '{ianaTimeZone}' không hợp lệ hoặc không được hỗ trợ.");
        }
    }
    
    /// <summary>
    /// Convert DateTime từ UTC sang timezone chỉ định.
    /// DateTime truyền vào phải là UTC (DateTimeKind.Utc).
    /// </summary>
    public static DateTime ConvertFromUtc(DateTime utcDateTime, string? ianaTimeZone)
    {
        if (string.IsNullOrWhiteSpace(ianaTimeZone)) return utcDateTime; // Giả sử đã là local

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZone);

            var utcSpecified = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(utcSpecified, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"Timezone '{ianaTimeZone}' không hợp lệ hoặc không được hỗ trợ.");
        }
    }
}