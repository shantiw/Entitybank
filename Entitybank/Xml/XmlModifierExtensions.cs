using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Helpers;
using XData.Data.Modification;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.Xml
{
    public static class XmlModifierExtensions
    {
        public static XElement CreateAndReturnKeys(this Modifier<XElement> modifier, XElement element, XElement schema)
        {
            Create(modifier, element, schema, out IEnumerable<Dictionary<string, object>> keys);

            XElement entitySchema;
            if (modifier.IsCollection(element))
            {
                entitySchema = schema.GetEntitySchemaByCollection(element.Name.LocalName);
            }
            else
            {
                entitySchema = schema.GetEntitySchema(element.Name.LocalName);
            }

            return keys.CreateReturnKeysToXml(entitySchema);
        }

        public static void Create(this Modifier<XElement> modifier, XElement element, XElement schema, out IEnumerable<Dictionary<string, object>> keys)
        {
            Create(modifier, element, schema);
            keys = modifier.GetCreateResult();
            modifier.Clear();
        }

        private static void Create(Modifier<XElement> modifier, XElement element, XElement schema)
        {
            if (modifier.IsCollection(element))
            {
                foreach (XElement child in element.Elements())
                {
                    AppendCreate(modifier, child, schema);
                }
            }
            else
            {
                AppendCreate(modifier, element, schema);
            }

            modifier.Validate();
            modifier.Persist();
        }

        public static void Delete(this Modifier<XElement> modifier, XElement element, XElement schema)
        {
            if (modifier.IsCollection(element))
            {
                foreach (XElement child in element.Elements())
                {
                    AppendDelete(modifier, child, schema);
                }
            }
            else
            {
                AppendDelete(modifier, element, schema);
            }

            modifier.Validate();
            modifier.Persist();
            modifier.Clear();
        }

        public static void Update(this Modifier<XElement> modifier, XElement element, XElement schema)
        {
            if (modifier.IsCollection(element))
            {
                foreach (XElement child in element.Elements())
                {
                    AppendUpdate(modifier, child, schema);
                }
            }
            else
            {
                AppendUpdate(modifier, element, schema);
            }

            modifier.Validate();
            modifier.Persist();
            modifier.Clear();
        }

        public static void Update(this Modifier<XElement> modifier, XElement element, XElement original, XElement schema)
        {
            IEnumerable<KeyValuePair<XElement, XElement>> pairs;
            if (modifier.IsCollection(element))
            {
                if (!modifier.IsCollection(original)) throw new ArgumentException(ErrorMessages.OriginalNotMatch, "original");

                XElement entitySchema = schema.GetEntitySchemaByCollection(element.Name.LocalName);
                XElement keySchema = SchemaHelper.GetKeySchema(entitySchema);
                pairs = modifier.Match(element.Elements(), original.Elements(), keySchema);
            }
            else
            {
                if (modifier.IsCollection(original)) throw new ArgumentException(ErrorMessages.OriginalNotMatch, "original");

                XElement keySchema = schema.GetKeySchema(element.Name.LocalName);
                pairs = modifier.Match(new List<XElement>() { element }, new List<XElement>() { original }, keySchema);
            }

            foreach (KeyValuePair<XElement, XElement> pair in pairs)
            {
                AppendUpdate(modifier, pair.Key, pair.Value, schema);
            }

            modifier.Validate();
            modifier.Persist();
            modifier.Clear();
        }

        public static void AppendCreate(this Modifier<XElement> modifier, XElement element, XElement schema)
        {
            modifier.AppendCreate(element, element.Name.LocalName, schema);
        }

        public static void AppendDelete(this Modifier<XElement> modifier, XElement element, XElement schema)
        {
            modifier.AppendDelete(element, element.Name.LocalName, schema);
        }

        public static void AppendUpdate(this Modifier<XElement> modifier, XElement element, XElement schema)
        {
            modifier.AppendUpdate(element, element.Name.LocalName, schema);
        }

        public static void AppendUpdate(this Modifier<XElement> modifier, XElement element, XElement original, XElement schema)
        {
            modifier.AppendUpdate(element, original, element.Name.LocalName, schema);
        }

        // overload
        public static XElement CreateAndReturnKeys(this Modifier<XElement> modifier, XElement element)
        {
            return CreateAndReturnKeys(modifier, element, modifier.Schema);
        }

        // overload
        public static void Create(this Modifier<XElement> modifier, XElement element, out IEnumerable<Dictionary<string, object>> keys)
        {
            Create(modifier, element, modifier.Schema, out keys);
        }

        // overload
        public static void Create(this Modifier<XElement> modifier, XElement element)
        {
            Create(modifier, element, modifier.Schema);
            modifier.Clear();
        }

        // overload
        public static void Delete(this Modifier<XElement> modifier, XElement element)
        {
            Delete(modifier, element, modifier.Schema);
        }

        // overload
        public static void Update(this Modifier<XElement> modifier, XElement element)
        {
            Update(modifier, element, modifier.Schema);
        }

        // overload
        public static void AppendCreate(this Modifier<XElement> modifier, XElement element)
        {
            AppendCreate(modifier, element, modifier.Schema);
        }

        // overload
        public static void AppendDelete(this Modifier<XElement> modifier, XElement element)
        {
            AppendDelete(modifier, element, modifier.Schema);
        }

        // overload
        public static void AppendUpdate(this Modifier<XElement> modifier, XElement element)
        {
            AppendUpdate(modifier, element, modifier.Schema);
        }


    }
}
