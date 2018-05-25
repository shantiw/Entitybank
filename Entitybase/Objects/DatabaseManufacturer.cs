using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Helpers;

namespace XData.Data.Objects
{
    internal class DatabaseManufacturer
    {
        private static Dictionary<string, XElement> Cache = new Dictionary<string, XElement>();
        private static object LockObj = new object();

        public void Update(string name)
        {
            XElement config = InstanceConfigGetter.GetConfig(name);
            lock (LockObj)
            {
                Cache[name] = config;
            }
        }

        // default SqlDatabase
        internal Database Create(string name)
        {
            if (!Cache.ContainsKey(name))
            {
                Update(name);
            }

            if (Cache[name].Element("database") == null)
            {
                string connectionString = ConfigurationManager.ConnectionStrings[name].ConnectionString;
                return new SqlDatabase(connectionString);
            }

            XElement dbConfig = new XElement(Cache[name].Element("database"));
            XElement xConnectionStringName = dbConfig.Element("connectionStringName");
            string type = dbConfig.Attribute("type").Value.Split(',')[0].Trim();
            switch (type)
            {
                case "XData.Data.DataObjects.SqlDatabase":
                    string connectionStringName = (xConnectionStringName == null) ? name : xConnectionStringName.Attribute("value").Value;
                    return new SqlDatabase(ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString);
                default:
                    break;
            }

            if (xConnectionStringName == null)
            {
                string connectionString = ConfigurationManager.ConnectionStrings[name].ConnectionString;
                XElement xConnectionString = new XElement("connectionString");
                xConnectionString.SetAttributeValue("type", "System.String");
                xConnectionString.SetAttributeValue("value", connectionString);
                dbConfig.Add(xConnectionString);
            }
            else
            {
                string connectionString = ConfigurationManager.ConnectionStrings[xConnectionStringName.Attribute("value").Value].ConnectionString;
                xConnectionStringName.Name = "connectionString";
                xConnectionStringName.SetAttributeValue("value", connectionString);
            }

            ObjectCreator objectCreator = new ObjectCreator(dbConfig);
            object obj = objectCreator.CreateInstance();

            return obj as Database;
        }


    }
}
