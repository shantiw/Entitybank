using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Helpers;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.Modification
{
    public static class ModificationExtensions
    {
        public static string CreateReturnKeysToJson(this IEnumerable<Dictionary<string, object>> keys)
        {
            List<string> jsons = new List<string>();
            foreach (Dictionary<string, object> key in keys)
            {
                List<string> keyValues = new List<string>();
                foreach (KeyValuePair<string, object> pair in key)
                {
                    Type type = pair.Value.GetType();
                    string value;
                    if (TypeHelper.IsNumeric(type))
                    {
                        value = pair.Value.ToString();
                    }
                    else if (type == typeof(bool))
                    {
                        value = ((bool)pair.Value) ? "true" : "false";
                    }
                    else if (type == typeof(DateTime))
                    {
                        value = new JsonNETFormatter().Format((DateTime)pair.Value);
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

        // overload
        public static XElement CreateReturnKeysToXml(this IEnumerable<Dictionary<string, object>> keys, string entity, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(entity);
            return CreateReturnKeysToXml(keys, entitySchema);
        }

        public static XElement CreateReturnKeysToXml(this IEnumerable<Dictionary<string, object>> keys, XElement entitySchema)
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
                        property.Value = new DotNETDateFormatter().Format((DateTime)pair.Value);
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


    }
}
