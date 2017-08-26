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

        public XmlDatabase Database { get; private set; }

        //protected DynModifier(Database<XElement> database, XElement schema) : base(database, schema)
        protected XmlModifier(XmlDatabase database, XElement schema) : base(database, schema)
        {
            Database = database;
        }

        public virtual void Create(XElement element, out IEnumerable<Dictionary<string, object>> result)
        {
            Create(element);
            result = GetCreateResult();
        }

        public virtual void Create(XElement element)
        {
            if (IsCollection(element))
            {
                foreach (XElement child in element.Elements())
                {
                    AppendCreate(child, child.Name.LocalName);
                }
            }
            else
            {
                AppendCreate(element, element.Name.LocalName);
            }

            Persist();
        }

        public virtual void Delete(XElement element)
        {
            if (IsCollection(element))
            {
                foreach (XElement child in element.Elements())
                {
                    AppendDelete(child, child.Name.LocalName);
                }
            }
            else
            {
                AppendDelete(element, element.Name.LocalName);
            }

            Persist();
        }

        public virtual void Update(XElement element)
        {
            if (IsCollection(element))
            {
                foreach (XElement child in element.Elements())
                {
                    AppendUpdate(child, child.Name.LocalName);
                }
            }
            else
            {
                AppendUpdate(element, element.Name.LocalName);
            }

            Persist();
        }

        protected override bool IsCollection(XElement element)
        {
            return element.Elements().All(x => x.HasElements);
        }

        public override void AppendCreate(XElement aggreg, string entity)
        {
            ExecuteAggregations.Add(new XmlCreateAggregation(aggreg, entity, Schema));
        }

        public override void AppendDelete(XElement aggreg, string entity)
        {
            ExecuteAggregations.Add(new XmlDeleteAggregation(aggreg, entity, Schema));
        }

        public override void AppendUpdate(XElement aggreg, string entity)
        {
            ExecuteAggregations.Add(new XmlUpdateAggregation(aggreg, entity, Schema));
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

        internal protected override Dictionary<string, object> GetPropertyValues(XElement obj, string entity)
        {
            XElement entitySchema = GetEntitySchema(Schema, entity);
            return new ExecuteAggregationHelper().GetPropertyValues(obj, entitySchema);
        }

        public static XmlModifier Create(string name, XElement schema)
        {
            XmlDatabase database = new XmlDatabase(CreateDatabase(name));
            return new XmlModifier(database, schema);
        }


    }
}
