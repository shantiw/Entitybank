using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Modification;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.Services
{
    public abstract class ModificationService<T>
    {
        protected const string DATE_FORMAT = "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";
        protected readonly XElement Schema;
        protected Modifier<T> Modifier;

        public ModificationService(string name, IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            Schema = GetSchema(name, keyValues);
        }

        public void Create(T obj, string entity, out XElement result)
        {
            Modifier.Create(obj, entity, out IEnumerable<Dictionary<string, object>> keys);
            result = KeysToXml(keys, GetEntitySchema(Schema, entity));
        }

        // json
        public void Create(T obj, string entity, out string result)
        {
            Modifier.Create(obj, entity, out IEnumerable<Dictionary<string, object>> keys);
            result = KeysToJson(keys);
        }

        public void Delete(T obj, string entity)
        {
            Modifier.Delete(obj, entity);
        }

        public void Update(T obj, string entity)
        {
            Modifier.Update(obj, entity);
        }

        protected string KeysToJson(IEnumerable<Dictionary<string, object>> keys)
        {
            List<string> jsons = new List<string>();
            foreach (Dictionary<string, object> key in keys)
            {
                List<string> keyValues = new List<string>();
                foreach (KeyValuePair<string, object> pair in key)
                {
                    Type type = pair.Value.GetType();
                    string value;
                    if (IsNumeric(type))
                    {
                        value = pair.Value.ToString();
                    }
                    else if (type == typeof(bool))
                    {
                        value = ((bool)pair.Value) ? "true" : "false";
                    }
                    else if (type == typeof(DateTime))
                    {
                        value = "\"" + ((DateTime)pair.Value).ToString(DATE_FORMAT) + "\"";
                    }
                    else
                    {
                        value = "\"" + pair.Value.ToString() + "\"";
                    }
                    keyValues.Add("\"" + pair.Key + "\":" + value);
                }
                jsons.Add("{" + string.Join(",", keyValues) + "}");
            }

            if (jsons.Count == 1) return jsons.First();

            return "[" + string.Join(",", jsons) + "]";
        }

        protected XElement KeysToXml(IEnumerable<Dictionary<string, object>> keys, XElement entitySchema)
        {
            string entity = entitySchema.Attribute(SchemaVocab.Name).Value;

            List<XElement> elements = new List<XElement>();
            foreach (Dictionary<string, object> key in keys)
            {
                XElement element = new XElement(entity);
                foreach (KeyValuePair<string, object> pair in key)
                {
                    XElement property = new XElement(pair.Key);
                    Type type = pair.Value.GetType();
                    if (type == typeof(DateTime))
                    {
                        property.Value = ((DateTime)pair.Value).ToString(DATE_FORMAT);
                    }
                    else if (type == typeof(bool))
                    {
                        property.Value = ((bool)pair.Value) ? "true" : "false";
                    }
                    else
                    {
                        property.Value = pair.Value.ToString();
                    }
                    element.Add(property);
                }
                elements.Add(element);
            }

            if (elements.Count == 1) return elements.First();

            string collection = entitySchema.Attribute(SchemaVocab.Collection).Value;
            XElement result = new XElement(collection);
            result.Add(elements);
            return result;
        }

        protected static XElement GetSchema(string name, IEnumerable<KeyValuePair<string, string>> deltaKey)
        {
            SchemaProvider schemaProvider = new SchemaProvider(name);
            return schemaProvider.GetSchema(deltaKey);
        }

        protected static XElement GetEntitySchema(XElement schema, string entity)
        {
            return schema.Elements(SchemaVocab.Entity).First(x => x.Attribute(SchemaVocab.Name).Value == entity);
        }

        protected static XElement GetEntitySchemaByCollection(XElement schema, string collection)
        {
            return schema.Elements(SchemaVocab.Entity).FirstOrDefault(x => x.Attribute(SchemaVocab.Collection).Value == collection);
        }

        protected static bool IsNumeric(Type type)
        {
            return (type == typeof(SByte) || type == typeof(Int16) || type == typeof(Int32) || type == typeof(Int64) ||
                type == typeof(Byte) || type == typeof(UInt16) || type == typeof(UInt32) || type == typeof(UInt64) ||
                type == typeof(Decimal) || type == typeof(Single) || type == typeof(Double));
        }


    }
}
