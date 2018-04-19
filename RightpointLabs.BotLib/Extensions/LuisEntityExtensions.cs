using Microsoft.Bot.Builder.Luis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RightpointLabs.BotLib.Extensions
{
    public static class LuisEntityExtensions
    {
        public static TimeSpan? ParseDuration(this LuisResult result)
        {
            var duration = result.Entities
                .Where(i => i.Type == "builtin.datetimeV2.duration")
                .SelectMany(i => (List<object>)i.Resolution["values"])
                .Select(i => ParseDuration((IDictionary<string, object>)i))
                .FirstOrDefault(i => i.HasValue);
            return duration;
        }

        public static DateTimeOffset[] ParseTime(this LuisResult result, TimeZoneInfo timezone)
        {
            var time = result.Entities
                .Where(i => i.Type == "builtin.datetimeV2.time" || i.Type == "builtin.datetimeV2.datetime")
                .Select(x =>
                {
                    var values = ((List<object>)x.Resolution["values"])
                        .Select(i => ParseTime((IDictionary<string, object>)i, timezone))
                        .Where(i => i.HasValue)
                        .ToArray();
                    if (values.Length > 1 && values.Count(i => i > DateTime.Now) != values.Length)
                    {
                        values = values.Where(i => i > DateTimeOffset.Now).ToArray();
                    }
                    return values.FirstOrDefault();
                })
                .Where(i => i.HasValue)
                .Select(i => i.Value)
                .ToArray();
            return time;
        }

        public static (DateTimeOffset start, DateTimeOffset end)? ParseTimeRange(this LuisResult result, TimeZoneInfo timezone)
        {
            var timeRange = result.Entities
                .Where(i => i.Type == "builtin.datetimeV2.timerange")
                .SelectMany(i => (List<object>)i.Resolution["values"])
                .Select(i => ParseTimeRange((IDictionary<string, object>)i, timezone))
                .FirstOrDefault(i => i.HasValue);
            return timeRange;
        }

        public static (DateTimeOffset start, DateTimeOffset end)? ParseTimeRange(IDictionary<string, object> values, TimeZoneInfo timezone)
        {
            switch ((string)values["type"])
            {
                case "timerange":
                    var start = DateTime.Parse((string)values["start"]).InTimeZone(timezone);
                    var end = DateTime.Parse((string)values["end"]).InTimeZone(timezone);
                    return (start, end);
                default:
                    return null;
            }
        }

        public static DateTimeOffset? ParseTime(IDictionary<string, object> values, TimeZoneInfo timezone)
        {
            switch ((string)values["type"])
            {
                case "time":
                case "datetime":
                    var value = DateTime.Parse((string)values["value"]);
                    if (values.TryGetValue("timex", out object timex))
                    {
                        if (timex is DateTime utcTime)
                        {
                            return TimeZoneInfo.ConvertTime(utcTime.InTimeZone(TimeZoneInfo.Utc), timezone);
                        }
                        else if (timex is string timexStr && timexStr == "PRESENT_REF")
                        {
                            return TimeZoneInfo.ConvertTime(value.InTimeZone(TimeZoneInfo.Utc), timezone);
                        }
                    }
                    return value.InTimeZone(timezone);
                default:
                    return null;
            }
        }

        public static TimeSpan? ParseDuration(IDictionary<string, object> values)
        {
            switch ((string)values["type"])
            {
                case "duration":
                    return TimeSpan.FromSeconds(int.Parse((string)values["value"]));
                default:
                    return null;
            }
        }
    }
}
