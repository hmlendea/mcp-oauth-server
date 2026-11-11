using System;
using System.Globalization;

namespace McpOAuthServer.Service.Mapping
{
    internal static class TimestampConverter
    {
        internal static DateTimeOffset Parse(string timestamp)
            => DateTimeOffset.Parse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        internal static string Format(DateTimeOffset timestamp)
            => timestamp.ToString("o", CultureInfo.InvariantCulture);
    }
}
