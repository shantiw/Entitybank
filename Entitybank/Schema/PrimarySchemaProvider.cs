using XData.Data.Helpers;
using XData.Data.Objects;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Reflection;

namespace XData.Data.Schema
{
    internal class CachedSchema
    {
        public XElement DbSchema { get; set; }
        public XElement RevisedDbSchema { get; set; }
        public XElement MappedSchema { get; set; }
        public XElement RevisedSchema { get; set; }

        public CachedSchema()
        {
        }

        public CachedSchema(CachedSchema cached)
        {
            DbSchema = new XElement(cached.DbSchema);
            RevisedDbSchema = new XElement(cached.RevisedDbSchema);
            MappedSchema = new XElement(cached.MappedSchema);
            RevisedSchema = new XElement(cached.RevisedSchema);
        }

    }

    // name: ConnectionStringName
    public class PrimarySchemaProvider
    {
        private static Dictionary<string, CachedSchema> Cache = new Dictionary<string, CachedSchema>();
        private static object LockObj = new object();

        private XElement GetConfig(string name)
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

        // default SqlSchemaProvider
        private IDbSchemaProvider GetDbSchemaProvider(string name, XElement config)
        {
            if (config.Element("dbSchemaProvider") == null)
            {
                string connectionString = ConfigurationManager.ConnectionStrings[name].ConnectionString;
                return new SqlSchemaProvider(connectionString);
            }

            XElement xDbSchemaProvider = new XElement(config.Element("dbSchemaProvider"));
            XElement xConnectionStringName = xDbSchemaProvider.Element("connectionStringName");
            string type = xDbSchemaProvider.Attribute("type").Value.Split(',')[0].Trim();
            switch (type)
            {
                case "XData.Data.Schema.SqlSchemaProvider":
                    string connectionStringName = (xConnectionStringName == null) ? name : xConnectionStringName.Attribute("value").Value;
                    return new SqlSchemaProvider(ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString);
                default:
                    break;
            }

            if (xConnectionStringName == null)
            {
                string connectionString = ConfigurationManager.ConnectionStrings[name].ConnectionString;
                XElement xConnectionString = new XElement("connectionString");
                xConnectionString.SetAttributeValue("type", "System.String");
                xConnectionString.SetAttributeValue("value", connectionString);
                xDbSchemaProvider.Add(xConnectionString);
            }
            else
            {
                string connectionString = ConfigurationManager.ConnectionStrings[xConnectionStringName.Attribute("value").Value].ConnectionString;
                xConnectionStringName.Name = "connectionString";
                xConnectionStringName.SetAttributeValue("value", connectionString);
            }

            ObjectCreator objectCreator = new ObjectCreator(xDbSchemaProvider);
            object obj = objectCreator.CreateInstance();

            return obj as IDbSchemaProvider;
        }

        // default DbSchemaReviser
        private IDbSchemaReviser GetDbSchemaReviser(XElement config)
        {
            XElement xDbSchemaReviser = config.Element("dbSchemaReviser");
            if (xDbSchemaReviser == null)
            {
                return new DbSchemaReviser();
            }

            string type = xDbSchemaReviser.Attribute("type").Value.Split(',')[0].Trim();
            switch (type)
            {
                case "XData.Data.Schema.SequenceDbSchemaReviser":
                    string format = xDbSchemaReviser.Element("format").Attribute("value").Value;
                    return new SequenceDbSchemaReviser(format);
                default:
                    break;
            }

            ObjectCreator objectCreator = new ObjectCreator(xDbSchemaReviser);
            object obj = objectCreator.CreateInstance();

            return obj as IDbSchemaReviser;
        }

        // default PrefixSuffixMapper
        private IMapper GetMapper(XElement config)
        {
            XElement xMapper = config.Element("mapper");
            if (xMapper == null)
            {
                return new PrefixSuffixMapper();
            }

            string type = xMapper.Attribute("type").Value.Split(',')[0].Trim();
            switch (type)
            {
                case "XData.Data.Schema.PrefixSuffixMapper":
                    return new PrefixSuffixMapper();
                case "XData.Data.Schema.PluralMapper":
                    return new PluralMapper();
                case "XData.Data.Schema.UnderscorePreSufMapper":
                    return new UnderscorePreSufMapper();

                case "XData.Data.Schema.ConfigPreSufMapper":
                    {
                        string fileName = xMapper.Element("fileName").Attribute("value").Value;
                        XElement xPrefix = xMapper.Element("prefix");
                        XElement xSuffix = xMapper.Element("suffix");
                        if (xPrefix != null && xSuffix != null)
                        {
                            return new ConfigPreSufMapper(xPrefix.Attribute("value").Value, xSuffix.Attribute("value").Value, fileName);
                        }
                        else
                        {
                            return new ConfigPreSufMapper(fileName);
                        }
                    }
                case "XData.Data.Schema.ConfigPluralMapper":
                    {
                        string fileName = xMapper.Element("fileName").Attribute("value").Value;
                        return new ConfigPluralMapper(fileName);
                    }
                case "XData.Data.Schema.ConfigUnderscorePreSufMapper":
                    {
                        string fileName = xMapper.Element("fileName").Attribute("value").Value;
                        XElement xPrefix = xMapper.Element("prefix");
                        XElement xSuffix = xMapper.Element("suffix");
                        if (xPrefix != null && xSuffix != null)
                        {
                            return new ConfigUnderscorePreSufMapper(xPrefix.Attribute("value").Value, xSuffix.Attribute("value").Value, fileName);
                        }
                        else
                        {
                            return new ConfigUnderscorePreSufMapper(fileName);
                        }
                    }
                default:
                    break;
            }

            ObjectCreator objectCreator = new ObjectCreator(xMapper);
            object obj = objectCreator.CreateInstance();

            return obj as IMapper;
        }

        // default SchemaReviser
        private ISchemaReviser GetSchemaReviser(XElement config)
        {
            XElement xSchemaReviser = config.Element("schemaReviser");
            if (xSchemaReviser == null)
            {
                return new SchemaReviser();
            }

            string type = xSchemaReviser.Attribute("type").Value.Split(',')[0].Trim();
            switch (type)
            {
                case "XData.Data.Schema.SchemaReviser":
                    return new SchemaReviser();
                case "XData.Data.Schema.ConfigSchemaReviser":
                    string fileName = xSchemaReviser.Element("fileName").Attribute("value").Value;
                    return new ConfigSchemaReviser(fileName);
                default:
                    break;
            }

            ObjectCreator objectCreator = new ObjectCreator(xSchemaReviser);
            object obj = objectCreator.CreateInstance();

            return obj as ISchemaReviser;
        }

        private CachedSchema NewCachedSchema(string name, SchemaSource source)
        {
            XElement config = GetConfig(name);

            CachedSchema cachedSchema;
            if (source == SchemaSource.DbSchemaProvider)
            {
                cachedSchema = new CachedSchema()
                {
                    DbSchema = GetDbSchemaProvider(name, config).GetDbSchema()
                };
            }
            else
            {
                cachedSchema = new CachedSchema(Cache[name]);
            }
            if (source <= SchemaSource.DbSchemaReviser)
            {
                cachedSchema.RevisedDbSchema = GetDbSchemaReviser(config).Revise(cachedSchema.DbSchema);
            }
            if (source <= SchemaSource.Mapper)
            {
                cachedSchema.MappedSchema = GetMapper(config).Map(cachedSchema.RevisedDbSchema);
            }
            if (source <= SchemaSource.SchemaReviser)
            {
                XElement revisedSchema = GetSchemaReviser(config).Revise(cachedSchema.MappedSchema);
                DateTime utcNow = DateTime.UtcNow;
                cachedSchema.RevisedSchema = revisedSchema;
            }

            return cachedSchema;
        }

        public XElement GetSchema(string name)
        {
            return GetSchema(name, SchemaSource.SchemaReviser);
        }

        public XElement GetSchema(string name, SchemaSource source)
        {
            if (!Cache.ContainsKey(name))
            {
                Update(name, SchemaSource.DbSchemaProvider);
            }
            switch (source)
            {
                case SchemaSource.DbSchemaProvider:
                    return new XElement(Cache[name].DbSchema);
                case SchemaSource.DbSchemaReviser:
                    return new XElement(Cache[name].RevisedDbSchema);
                case SchemaSource.Mapper:
                    return new XElement(Cache[name].MappedSchema);
                case SchemaSource.SchemaReviser:
                    return new XElement(Cache[name].RevisedSchema);
            }

            throw new NotSupportedException(); // never
        }

        public void Update(string name, SchemaSource source)
        {
            CachedSchema cachedSchema = NewCachedSchema(name, source);
            lock (LockObj)
            {
                Cache[name] = cachedSchema;
            }
        }


    }
}
