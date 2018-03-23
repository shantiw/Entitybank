using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Modification;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.Dynamic
{
    // on the one hand as an universal Modifier, on the other hand can avoid references Newtonsoft.Json
    internal class _DynModifier : Modifier<dynamic>
    {
        public _DynModifier(Database<dynamic> database, XElement schema) : base(database, schema)
        {
        }

        public override void AppendCreate(dynamic aggreg, string entity, XElement schema)
        {
            ExecuteAggregations.Add(new DynCreateAggregation(aggreg, entity, schema));
        }

        public override void AppendDelete(dynamic aggreg, string entity, XElement schema)
        {
            ExecuteAggregations.Add(new DynDeleteAggregation(aggreg, entity, schema));
        }

        public override void AppendUpdate(dynamic aggreg, string entity, XElement schema)
        {
            ExecuteAggregations.Add(new DynUpdateAggregation(aggreg, entity, schema));
        }

        internal protected override bool IsCollection(dynamic obj)
        {
            try
            {
                object o = obj[0];
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal protected override dynamic CreateObject(Dictionary<string, object> propertyValues, string entity)
        {
            dynamic obj = new ExpandoObject();
            IDictionary<string, object> dict = obj as IDictionary<string, object>;
            foreach (KeyValuePair<string, object> pair in propertyValues)
            {
                dict[pair.Key] = pair.Value;
            }
            return obj;
        }

        internal protected override Dictionary<string, object> GetPropertyValues(dynamic obj, string entity, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(entity);
            return new ExecuteAggregationHelper().GetPropertyValues(obj, entitySchema);
        }

        internal protected override void SetObjectValue(dynamic obj, string property, object value)
        {
            if (value == null)
            {
                obj[property] = null;
                return;
            }

            Type type = value.GetType();
            if (type == typeof(string))
            {
                obj[property] = (string)value;
                return;
            }

            if (type == typeof(DateTime))
            {
                obj[property] = (DateTime)value;
                return;
            }

            if (type == typeof(bool))
            {
                obj[property] = (bool)value;
                return;
            }

            if (type == typeof(SByte))
            {
                obj[property] = (SByte)value;
                return;
            }
            if (type == typeof(Int16))
            {
                obj[property] = (Int16)value;
                return;
            }
            if (type == typeof(Int32))
            {
                obj[property] = (Int32)value;
                return;
            }
            if (type == typeof(Int64))
            {
                obj[property] = (Int64)value;
                return;
            }
            if (type == typeof(Byte))
            {
                obj[property] = (Byte)value;
                return;
            }
            if (type == typeof(UInt16))
            {
                obj[property] = (UInt16)value;
                return;
            }
            if (type == typeof(UInt32))
            {
                obj[property] = (UInt32)value;
                return;
            }
            if (type == typeof(UInt64))
            {
                obj[property] = (UInt64)value;
                return;
            }
            if (type == typeof(Decimal))
            {
                obj[property] = (Decimal)value;
                return;
            }
            if (type == typeof(Single))
            {
                obj[property] = (Single)value;
                return;
            }
            if (type == typeof(Double))
            {
                obj[property] = (Double)value;
                return;
            }
            if (type == typeof(Guid))
            {
                obj[property] = (Guid)value;
                return;
            }
            if (type == typeof(byte[]))
            {
                obj[property] = (byte[])value;
                return;
            }

            throw new NotSupportedException(type.ToString());
        }


    }

    public static class DynModifierFactory
    {
        public static Modifier<dynamic> Create(string name, XElement schema = null)
        {
            Database<dynamic> database = new _DynDatabase(new DatabaseManufacturer().Create(name));
            XElement xSchema = schema ?? new PrimarySchemaProvider().GetSchema(name);
            return new _DynModifier(database, xSchema);
        }
    }
}
