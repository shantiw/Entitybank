using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Modification;
using XData.Data.Schema;

namespace XData.Data.Xml
{
    public class ExecuteAggregationHelper : IExecuteAggregationHelper<XElement>, ICreateAggregationHelper<XElement>, IDeleteAggregationHelper<XElement>, IUpdateAggregationHelper<XElement>
    {
        public IEnumerable<XElement> GetChildren(XElement element)
        {
            return element.Elements();
        }

        public Dictionary<string, object> GetPropertyValues(XElement element, XElement entitySchema)
        {
            Dictionary<string, object> propertyValues = new Dictionary<string, object>();

            foreach (XElement child in element.Elements())
            {
                XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).FirstOrDefault(x =>
                    x.Attribute(SchemaVocab.Name).Value == child.Name.LocalName && x.Attribute(SchemaVocab.Column) != null);
                if (propertySchema == null) continue;

                string dataType = propertySchema.Attribute(SchemaVocab.DataType).Value;
                Type type = Type.GetType(dataType);

                object value = ChangeType(child.Value, type);

                propertyValues.Add(child.Name.LocalName, value);
            }

            return propertyValues;
        }

        public Dictionary<XElement, XElement> GetPropertySchemaChildrenDictionary(XElement element, XElement entitySchema)
        {
            Dictionary<XElement, XElement> dict = new Dictionary<XElement, XElement>();
            foreach (XElement child in element.Elements())
            {
                XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).FirstOrDefault(x =>
                    x.Attribute(SchemaVocab.Name).Value == child.Name.LocalName && x.Attribute(SchemaVocab.Collection) != null);
                if (propertySchema == null) continue;

                // preliminary screening
                dict.Add(propertySchema, child); // OneToMany
            }
            return dict;
        }

        public void ResetObjectValues(XElement element, Dictionary<string, object> propertyValues)
        {
            element.RemoveNodes();

            foreach (KeyValuePair<string, object> propertyValue in propertyValues)
            {
                if (propertyValue.Value == null)
                {
                    element.SetElementValue(propertyValue.Key, string.Empty);
                    //element.Element(propertyValue.Key).SetAttributeValue("", "true");
                }
                else
                {
                    element.SetElementValue(propertyValue.Key, propertyValue.Value);
                }

            }
        }

        public InsertCommand<XElement> CreateInsertCommand(XElement aggregNode, string entity, XElement schema, XElement aggreg)
        {
            return new InsertCommand<XElement>(aggregNode, entity, schema, aggreg);
        }

        public DeleteCommand<XElement> CreateDeleteCommand(XElement aggregNode, string entity, XElement schema, XElement aggreg)
        {
            return new DeleteCommand<XElement>(aggregNode, entity, schema, aggreg);
        }

        public UpdateCommand<XElement> CreateUpdateCommand(XElement aggregNode, string entity, XElement schema, XElement aggreg)
        {
            return new UpdateCommand<XElement>(aggregNode, entity, schema, aggreg);
        }

        // <element>
        // ...
        // <element.original>
        //  <property>value</property> ...
        // </element.original>
        // </element>
        public XElement GetOriginal(XElement element)
        {
            return element.Element(string.Format("{0}.{1}", element.Name.LocalName, SchemaVocab.Original));
        }

        public UpdateCommandNode<XElement> CreateUpdateCommandNode(XElement aggregNode, string entity, XElement schema, XElement aggreg)
        {
            return new UpdateCommandNode<XElement>(aggregNode, entity, schema, aggreg);
        }

        private static object ChangeType(string value, Type dataType)
        {
            if (dataType == typeof(string))
            {
                if (value == string.Empty)
                {
                    //element.Attribute() != null && element.Attribute().Value=="true" return null;

                    return value;
                }
                else
                {
                    return value;
                }
            }

            if (string.IsNullOrWhiteSpace(value)) return null;

            if (dataType == typeof(DateTime))
            {
                return DateTime.Parse(value);
            }

            if (dataType == typeof(bool))
            {
                return bool.Parse(value);
            }

            if (dataType == typeof(SByte))
            {
                return SByte.Parse(value);
            }
            if (dataType == typeof(Int16))
            {
                return Int16.Parse(value);
            }
            if (dataType == typeof(Int32))
            {
                return Int32.Parse(value);
            }
            if (dataType == typeof(Int64))
            {
                return Int64.Parse(value);
            }
            if (dataType == typeof(Byte))
            {
                return Byte.Parse(value);
            }
            if (dataType == typeof(UInt16))
            {
                return UInt16.Parse(value);
            }
            if (dataType == typeof(UInt32))
            {
                return UInt32.Parse(value);
            }
            if (dataType == typeof(UInt64))
            {
                return Int16.Parse(value);
            }
            if (dataType == typeof(Decimal))
            {
                return Decimal.Parse(value);
            }
            if (dataType == typeof(Single))
            {
                return Single.Parse(value);
            }
            if (dataType == typeof(Double))
            {
                return Double.Parse(value);
            }

            if (dataType == typeof(Guid))
            {
                return Guid.Parse(value);
            }

            if (dataType == typeof(byte[]))
            {
                return Convert.FromBase64String(value);
            }

            return value;
        }


    }
}
