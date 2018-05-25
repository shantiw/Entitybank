using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Helpers;

namespace XData.Data.Objects
{
    internal class DateFormatterManufacturer
    {
        private static Dictionary<string, XElement> Cache = null;
        private static object LockObj = new object();

        private static Dictionary<string, XElement> NewBuiltIn()
        {
            Dictionary<string, XElement> builtIn = new Dictionary<string, XElement>();

            XElement xBuiltIn = XElement.Parse(BuiltIn.DateFormatter);

            foreach (XElement xFormatter in xBuiltIn.Elements("dateFormatter"))
            {
                string name = xFormatter.Attribute("name").Value;
                builtIn[name] = xFormatter;
            }

            return builtIn;
        }

        private static Dictionary<string, XElement> NewCache()
        {
            Dictionary<string, XElement> cache = NewBuiltIn();

            string dir = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
            if (!Directory.Exists(dir))
            {
                dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
            }
            if (!Directory.Exists(dir)) return cache;

            string file = Path.Combine(dir, "global.config");
            if (!File.Exists(file)) return cache;

            XElement config = XElement.Load(file);

            foreach (XElement xFormatter in config.Elements("dateFormatter"))
            {
                string name = xFormatter.Attribute("name").Value;
                cache[name] = xFormatter;
            }

            return cache;
        }

        public void Update()
        {
            Dictionary<string, XElement> cache = NewCache();
            lock (LockObj)
            {
                Cache = cache;
            }
        }

        internal DateFormatter Create(string dateFormatter, TimeSpan timezoneOffset, string format = null)
        {
            if (Cache == null)
            {
                Update();
            }

            XElement xFormatter = Cache[dateFormatter];
            string type = xFormatter.Attribute("type").Value.Split(',')[0].Trim();
            switch (type)
            {
                case "XData.Data.Objects.DotNETDateFormatter":
                    return string.IsNullOrWhiteSpace(format) ? new DotNETDateFormatter() : new DotNETDateFormatter(format);
                case "XData.Data.Objects.JsonNETFormatter":
                    return new XData.Data.Objects.JsonNETFormatter() { TimezoneOffset = timezoneOffset };
                default:
                    break;
            }

            XElement xFormat = xFormatter.Element("format");
            if (xFormat != null && !string.IsNullOrWhiteSpace(format))
            {
                xFormat.SetAttributeValue("value", format);
            }

            ObjectCreator objectCreator = new ObjectCreator(xFormatter);
            object obj = objectCreator.CreateInstance();
            DateFormatter formatter = obj as DateFormatter;
            formatter.TimezoneOffset = timezoneOffset;
            return formatter;
        }


    }
}
