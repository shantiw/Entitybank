using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Helpers;

namespace XData.Data.Objects
{
    public abstract class DateFormatter
    {
        internal protected TimeSpan? TimezoneOffset { get; set; }
        public abstract string Format(DateTime value);
    }

    public sealed class DotNETDateFormatter : DateFormatter
    {
        public const string DEFAULT_FORMAT = TypeHelper.DEFAULT_DATE_FORMAT;
        public const string UTC_FORMAT = "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ";
        public const string DATE_FORMAT = "yyyy-MM-dd";
        public const string TIME_FORMAT = "HH:mm:ss.FFFFFFF";

        private readonly string _format = DEFAULT_FORMAT;

        public DotNETDateFormatter()
        {
        }

        public DotNETDateFormatter(string format)
        {
            _format = format;
        }

        public override string Format(DateTime value)
        {
            return value.ToString(_format);
        }

    }

    public sealed class JsonNETFormatter : DateFormatter
    {
        private static readonly long DatetimeMinTimeTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;

        public override string Format(DateTime value)
        {
            TimeSpan offset = (TimezoneOffset == null) ? TimeZoneInfo.Local.GetUtcOffset(value) : (TimeSpan)TimezoneOffset;

            StringBuilder sb = new StringBuilder();
            sb.Append(@"\/Date(");
            sb.Append((value.ToUniversalTime().Ticks - DatetimeMinTimeTicks) / 10000);
            sb.Append(ToString(offset));
            sb.Append(@")\/");

            return sb.ToString();
        }

        private string ToString(TimeSpan value)
        {
            string hhmm = value.ToString(@"hhmm");
            string sign = (value.Ticks < 0) ? "-" : "+";
            return sign + hhmm;
        }

    }

}
