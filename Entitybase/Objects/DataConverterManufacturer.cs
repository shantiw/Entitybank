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
    internal class DataConverterManufacturer
    {
        private static Dictionary<string, XElement> Cache = null;
        private static object LockObj = new object();

        private Dictionary<string, XElement> NewBuiltIn()
        {
            Dictionary<string, XElement> builtIn = new Dictionary<string, XElement>();

            XElement xBuiltIn = XElement.Parse(BuiltIn.DataConverter);

            foreach (XElement xFormatter in xBuiltIn.Elements("dataConverter"))
            {
                string name = xFormatter.Attribute("name").Value;
                builtIn[name] = xFormatter;
            }

            return builtIn;
        }

        private Dictionary<string, XElement> NewCache()
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

            foreach (XElement xFormatter in config.Elements("dataConverter"))
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

        internal DataConverter<T> Create<T>(string dataConverter)
        {
            if (Cache == null)
            {
                Update();
            }

            XElement xConverter = Cache[dataConverter];
            string type = xConverter.Attribute("type").Value.Split(',')[0].Trim();
            switch (type)
            {
                case "XData.Data.Objects.ToJsonConverter":
                    return new ToJsonConverter() as DataConverter<T>;
                case "XData.Data.Objects.ToXmlConverter":
                    return new ToXmlConverter() as DataConverter<T>;
                default:
                    break;
            }

            ObjectCreator objectCreator = new ObjectCreator(xConverter);
            object obj = objectCreator.CreateInstance();

            return obj as DataConverter<T>;
        }


    }
}
