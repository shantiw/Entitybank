using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.OData;
using XData.Data.Schema;

namespace XData.Data.Services
{
    // name: ConnectionStringName
    public class ODataService
    {
        public static DateTime GetNow(string name)
        {
            return ODataQuerier.GetNow(name);
        }

        public static DateTime GetUtcNow(string name)
        {
            return ODataQuerier.GetUtcNow(name);
        }

        public static int Count(string name, string collection, IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            XElement schema = GetSchema(name, GetDeltaSchemaKey(keyValues));
            string filter = GetValue(keyValues, "$filter");
            return ODataQuerier.Count(name, schema, GetEntity(schema, collection), filter, GetParameterValues(keyValues));
        }

        protected static string GetValue(IEnumerable<KeyValuePair<string, string>> nameValues, string key)
        {
            string value = null;
            if (nameValues.Any(p => p.Key == key))
            {
                value = nameValues.First(p => p.Key == key).Value;
            }
            return value;
        }

        protected static IEnumerable<KeyValuePair<string, string>> GetDeltaSchemaKey(IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            return keyValues.Where(p => !p.Key.StartsWith("$") && !p.Key.StartsWith("@") && p.Key != "covn" && p.Key != "date");
        }

        protected static IEnumerable<KeyValuePair<string, string>> GetParameterValues(IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            return keyValues.Where(p => p.Key.StartsWith("@"));
        }

        protected static XElement GetSchema(string name, IEnumerable<KeyValuePair<string, string>> deltaKey)
        {
            SchemaProvider schemaProvider = new SchemaProvider(name);
            return schemaProvider.GetSchema(deltaKey);
        }

        public static string GetEntity(XElement schema, string collection)
        {
            XElement entitySchema = schema.Elements(SchemaVocab.Entity).FirstOrDefault(x => x.Attribute(SchemaVocab.Collection).Value == collection);
            return entitySchema.Attribute(SchemaVocab.Name).Value;
        }


    }
}
