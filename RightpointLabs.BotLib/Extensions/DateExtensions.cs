using System;

namespace RightpointLabs.BotLib.Extensions
{
    public static class DateExtensions
    {
        public static DateTimeOffset InTimeZone(this DateTime value, TimeZoneInfo tz)
        {
            return new DateTimeOffset(value, tz.GetUtcOffset(value));
        }
    }
}