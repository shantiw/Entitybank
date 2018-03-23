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
            }

            return element;
        }

        internal protected override Dictionary<string, object> GetPropertyValues(XElement obj, string entity, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(entity);
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
