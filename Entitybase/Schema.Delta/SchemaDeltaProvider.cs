using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Helpers;

namespace XData.Data.Schema
{
    // name: ConnectionStringName
    public class SchemaDeltaProvider
    {
        private static Dictionary<string, SchemaDeltaFinder> Cache = new Dictionary<string, SchemaDeltaFinder>();
        private static object LockObj = new object();

        public void Update(string name)
        {
            lock (LockObj)
            {
                Cache[name] = CreateSchemaDeltaFinder(name);
            }
        }

        protected SchemaDeltaFinder CreateSchemaDeltaFinder(string name)
        {
            XElement config = InstanceConfigGetter.GetConfig(name);
            if (config.Element("schemaDeltaFinder") == null)
            {
                return new ConfigSchemaDeltaFinder(name, string.Empty, string.Empty);
            }

            XElement xSchemaDeltaFinder = new XElement(config.Element("schemaDeltaFinder"));
            xSchemaDeltaFinder.Element("name").SetAttributeValue("value", name);

            string type = xSchemaDeltaFinder.Attribute("type").Value.Split(',')[0].Trim();
            switch (type)
            {
                case "XData.Data.Schema.ConfigSchemaDeltaFinder":
                    string excludedAttributes = xSchemaDeltaFinder.Element("excludedAttributes").Attribute("value").Value;
                    string separator = xSchemaDeltaFinder.Element("separator").Attribute("value").Value;
                    return new ConfigSchemaDeltaFinder(name, excludedAttributes, separator);
                default:
                    break;
            }

            ObjectCreator objectCreator = new ObjectCreator(xSchemaDeltaFinder);
            object obj = objectCreator.CreateInstance();

            return obj as SchemaDeltaFinder;
        }

        // <delta key1="key1"> // /dev/schema/{id}?key1=key1&key2=
        // <delta key1="key1" key2="key2"> // /dev/schema/{id}?key1=key1&key2=key2
        public XElement Get(string name, IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            if (!Cache.ContainsKey(name))
            {
                Update(name);
            }

            SchemaDeltaFinder finder = Cache[name];
            return finder.Find(keyValues);
        }


    }
}
