using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public class XmlModifier : Modifier<XElement>
    {
        public event ValidatingEventHandler Validating;

        protected override void OnValidating(Execution execution, XElement aggreg, string entity, XElement schema,
            IEnumerable<ExecutionEntry<XElement>> context, out ICollection<ValidationResult> validationResults)
        {
            ValidatingEventArgs args = new ValidatingEventArgs(execution, aggreg, entity, schema, context);
            Validating?.Invoke(this, args);

            validationResults = args.ValidationResults;
        }

        public Database<XElement> Database { get; private set; }

        //protected XmlModifier(Database<XElement> database, XElement schema) : base(database, schema)
        public XmlModifier(Database<XElement> database, XElement schema) : base(database, schema)
        {
            Database = database;
        }

        // overload
        public XElement CreateAndReturnKeys(XElement element)
        {
            return CreateAndReturnKeys(element, Schema);
        }

        public XElement CreateAndReturnKeys(XElement element, XElement schema)
        {
            Create(element, schema, out IEnumerable<Dictionary<string, object>> result);
            XElement entitySchema;
            if (IsCollection(element))
            {
                entitySchema = GetEntitySchemaByCollection(schema, element.Name.LocalName);
            }
            else
            {
                entitySchema = GetEntitySchema(schema, element.Name.LocalName);
            }
            return KeysToXml(result, entitySchema);
        }

        // overload
        public void Create(XElement element, out IEnumerable<Dictionary<string, object>> keys)
        {
            Create(element, Schema, out keys);
        }

        // overload
        public void Create(XElement element, XElement schema, out IEnumerable<Dictionary<string, object>> keys)
        {
            Create(element, schema);
            keys = GetCreateResult();
            Clear();
        }

        // overload
        public void Create(XElement element)
        {
            Create(element, Schema);
            Clear();
        }

        protected void Create(XElement element, XElement schema)
        {
            if (IsCollection(element))
            {
                foreach (XElement child in element.Elements())
                {
                    AppendCreate(child, schema);
                }
            }
            else
            {
                AppendCreate(element, schema);
            }

            Validate();
            Persist();
        }

        // overload
        public void Delete(XElement element)
        {
            Delete(element, Schema);
        }

        public void Delete(XElement element, XElement schema)
        {
            if (IsCollection(element))
            {
                foreach (XElement child in element.Elements())
                {
                    AppendDelete(child, schema);
                }
            }
            else
            {
                AppendDelete(element, schema);
            }

            Validate();
            Persist();
            Clear();
        }

        // overload
        public void Update(XElement element)
        {
            Update(element, Schema);
        }

        public void Update(XElement element, XElement schema)
        {
            if (IsCollection(element))
            {
                foreach (XElement child in element.Elements())
                {
                    AppendUpdate(child, schema);
                }
            }
            else
            {
                AppendUpdate(element, schema);
            }

            Validate();
            Persist();
            Clear();
        }

        public void AppendCreate(XElement element)
        {
            AppendCreate(element, element.Name.LocalName);
        }

        public void AppendDelete(XElement element)
        {
            AppendDelete(element, element.Name.LocalName);
        }

        public void AppendUpdate(XElement element)
        {
            AppendUpdate(element, element.Name.LocalName);
        }

        // overload
        public void AppendCreate(XElement element, XElement schema)
        {
            AppendCreate(element, element.Name.LocalName, schema);
        }

        // overload
        public void AppendDelete(XElement element, XElement schema)
        {
            AppendDelete(element, element.Name.LocalName, schema);
        }

        // overload
        public void AppendUpdate(XElement element, XElement schema)
        {
            AppendUpdate(element, element.Name.LocalName, schema);
        }

        protected override bool IsCollection(XElement element)
        {
            return element.Elements().All(x => x.HasElements);
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

        internal protected override void SetObjectValue(XElement obj, string property, object value)
        {
            if (value == null)
            {
                obj.SetElementValue(property, string.Empty);
                //obj.Element(property).SetAttributeValue("", true);
                return;
            }

            obj.SetElementValue(property, value.ToString());
        }

        protected const string DATE_FORMAT = TypeHelper.DEFAULT_DATE_FORMAT;
        protected const string XSI = TypeHelper.XSI;
        protected static readonly XNamespace XSINamespace = TypeHelper.XSINamespace;

        protected static XElement KeysToXml(IEnumerable<Dictionary<string, object>> keys, XElement entitySchema)
        {
            return keys.CreateReturnKeysToXml(entitySchema);
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
            XElement entitySchema = GetEntitySchema(schema, entity);
            return new ExecuteAggregationHelper().GetPropertyValues(obj, entitySchema);
        }

        public static XmlModifier Create(string name, XElement schema = null)
        {
            Database<XElement> database = new _XmlDatabase(new DatabaseManufacturer().Create(name));
            XElement xSchema = schema ?? new PrimarySchemaProvider().GetSchema(name);
            return new XmlModifier(database, xSchema);
        }


    }
}
