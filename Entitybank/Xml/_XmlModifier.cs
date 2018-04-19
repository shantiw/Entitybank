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
    internal class _XmlModifier : Modifier<XElement>
    {
        protected const string DATE_FORMAT = TypeHelper.DEFAULT_DATE_FORMAT;
        protected const string XSI = TypeHelper.XSI;
        protected static readonly XNamespace XSINamespace = TypeHelper.XSINamespace;

        public _XmlModifier(Database<XElement> database, XElement schema) : base(database, schema)
        {
        }

        public override void AppendCreate(XElement aggreg, string entity, XElement schema)
        {
            ExecuteAggregations.Add(new XmlCreateAggregation(aggreg, entity, schema));
        }

        public override void AppendDelete(XElement aggreg, string entity, XElement schema)
        {
            ExecuteAggregations.Add(new XmlDeleteAggregation(aggreg, entity, schema));
        }

        public override void AppendUpdate(XElement aggreg, string entity, XElement schema)
        {
            ExecuteAggregations.Add(new XmlUpdateAggregation(aggreg, entity, schema));
        }

        public override void AppendUpdate(XElement aggreg, XElement original, string entity, XElement schema)
        {
            ExecuteAggregations.Add(new XmlUpdateAggregation(aggreg, original, entity, schema));
        }

        internal protected override bool IsCollection(XElement element)
        {
            return element.Elements().All(x => x.HasElements);
        }

        internal protected override XElement CreateObject(Dictionary<string, object> propertyValues, string entity)
        {
            XElement element = new XElement(entity);
            element.SetAttributeValue(XNamespace.Xmlns + "i", XSI);

            foreach (KeyValuePair<string, object> pair in propertyValues)
            {
                XElement xProperty = new XElement(pair.Key);

                if (pair.Value == null)
                {
                    xProperty.Value = string.Empty;
                    xProperty.SetAttributeValue(XSINamespace + "nil", "true");
                }
                else
                {
                    Type type = pair.Value.GetType();
                    if (type == typeof(DateTime))
                    {
                        xProperty.Value = ((bool)pair.Value) ? "true" : "false";
                    }
                    else if (type == typeof(DateTime))
                    {
                        xProperty.Value = ((DateTime)pair.Value).ToString(DATE_FORMAT);
                    }
                    else
                    {
                        xProperty.Value = pair.Value.ToString();
                    }
                }

                element.Add(xProperty);
            }

            return element;
        }

        internal protected override Dictionary<string, object> GetPropertyValues(XElement obj, XElement entitySchema)
        {
            return new ExecuteAggregationHelper().GetPropertyValues(obj, entitySchema);
        }

        internal protected override void SetObjectValue(XElement obj, string property, object value)
        {
            if (value == null)
            {
                obj.SetElementValue(property, string.Empty);
                obj.Element(property).SetAttributeValue(XSINamespace + "nil", "true");
                return;
            }

            obj.SetElementValue(property, value.ToString());
        }

        protected override IEnumerable<XElement> Filter(IEnumerable<XElement> elements, XElement key, XElement keySchema)
        {
            IEnumerable<XElement> xElements = elements;
            foreach (XElement propertySchema in keySchema.Elements())
            {
                string property = propertySchema.Attribute(SchemaVocab.Name).Value;
                string value = key.Element(property).Value;
                xElements = xElements.Where(p => p.Element(property).Value == value);
            }
            return xElements;
        }

        #region override
        protected override void Create(XElement obj, string entity, XElement schema)
        {
            XElement xSchema = schema ?? Schema;

            if (IsCollection(obj))
            {
                foreach (XElement child in obj.Elements())
                {
                    AppendCreate(child, entity, xSchema);
                }
            }
            else
            {
                AppendCreate(obj, entity, xSchema);
            }

            Validate();
            Persist();
        }

        public override void Delete(XElement obj, string entity, XElement schema = null)
        {
            XElement xSchema = schema ?? Schema;

            if (IsCollection(obj))
            {
                foreach (XElement child in obj.Elements())
                {
                    AppendDelete(child, entity, xSchema);
                }
            }
            else
            {
                AppendDelete(obj, entity, xSchema);
            }

            Validate();
            Persist();
            Clear();
        }

        public override void Update(XElement obj, string entity, XElement schema = null)
        {
            XElement xSchema = schema ?? Schema;

            if (IsCollection(obj))
            {
                foreach (XElement child in obj.Elements())
                {
                    AppendUpdate(child, entity, xSchema);
                }
            }
            else
            {
                AppendUpdate(obj, entity, xSchema);
            }

            Validate();
            Persist();
            Clear();
        }

        public override void Update(XElement obj, XElement original, string entity, XElement schema = null)
        {
            XElement xSchema = schema ?? Schema;
            XElement keySchema = schema.GetKeySchema(entity);

            IEnumerable<KeyValuePair<XElement, XElement>> pairs;
            if (IsCollection(obj))
            {
                if (!IsCollection(original)) throw new ArgumentException(ErrorMessages.OriginalNotMatch, "original");

                pairs = Match(obj.Elements(), original.Elements(), keySchema);
            }
            else
            {
                if (IsCollection(original)) throw new ArgumentException(ErrorMessages.OriginalNotMatch, "original");

                pairs = Match(new List<XElement>() { obj }, new List<XElement>() { original }, keySchema);
            }

            foreach (KeyValuePair<XElement, XElement> pair in pairs)
            {
                AppendUpdate(pair.Key, pair.Value, entity, xSchema);
            }

            Validate();
            Persist();
            Clear();
        }
        #endregion
    }

    public static class XmlModifierFactory
    {
        public static Modifier<XElement> Create(string name, XElement schema = null)
        {
            Database<XElement> database = new _XmlDatabase(new DatabaseManufacturer().Create(name));
            XElement xSchema = schema ?? new PrimarySchemaProvider().GetSchema(name);
            return new _XmlModifier(database, xSchema);
        }
    }
}
