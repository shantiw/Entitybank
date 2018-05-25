using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Helpers
{
    public static class InstanceConfigGetter
    {
        public static XElement GetConfig(string name)
        {
            XElement config = new XElement("configuration");

            string assemblyName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
            string dir = Path.Combine(assemblyName, name);
            if (!Directory.Exists(dir))
            {
                dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
            }
            if (!Directory.Exists(dir)) return config;

            string file = Path.Combine(dir, "instance.config");
            if (!File.Exists(file)) return config;

            return XElement.Load(file);
        }
    }
}
