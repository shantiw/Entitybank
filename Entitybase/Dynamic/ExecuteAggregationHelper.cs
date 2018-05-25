using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Modification;
using XData.Data.Schema;

namespace XData.Data.Dynamic
{
    public class ExecuteAggregationHelper : IExecuteAggregationHelper<object>, ICreateAggregationHelper<object>, IDeleteAggregationHelper<object>, IUpdateAggregationHelper<object>
    {
        public IEnumerable<dynamic> GetChildren(dynamic obj)
        {
            return obj as IEnumerable<dynamic>;
        }

        public Dictionary<string, object> GetPropertyValues(dynamic obj, XElement entitySchema)
        {
            Dictionary<string, object> propertyValues = new Dictionary<string, object>();

            IEnumerable<string> dynamicMemberNames = GetDynamicMemberNames(obj);
            foreach (string member in dynamicMemberNames)
            {
                XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).FirstOrDefault(x =>
                    x.Attribute(SchemaVocab.Name).Value == member && x.Attribute(SchemaVocab.Column) != null);
                if (propertySchema == null) continue;

                string dataType = propertySchema.Attribute(SchemaVocab.DataType).Value;
                Type conversionType = Type.GetType(dataType);

                dynamic value = obj[member];
                object val = ChangeType(value, conversionType);

                propertyValues.Add(member, val);
            }

            return propertyValues;
        }

        public IEnumerable<KeyValuePair<XElement, dynamic>> GetPropertySchemaChildrens(dynamic obj, XElement entitySchema)
        {
            List<KeyValuePair<XElement, dynamic>> list = new List<KeyValuePair<XElement, dynamic>>();
            IEnumerable<string> dynamicMemberNames = GetDynamicMemberNames(obj);
            foreach (string member in dynamicMemberNames)
            {
                XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).FirstOrDefault(x =>
                    x.Attribute(SchemaVocab.Name).Value == member && x.Attribute(SchemaVocab.Collection) != null);
                if (propertySchema == null) continue;

                // preliminary screening
                list.Add(new KeyValuePair<XElement, dynamic>(propertySchema, obj[member])); // OneToMany
            }
            return list;
        }

        public InsertCommand<dynamic> CreateInsertCommand(dynamic aggregNode, string entity, XElement schema, dynamic aggreg)
        {
            return new InsertCommand<dynamic>(aggregNode, entity, schema, aggreg);
        }

        public DeleteCommand<dynamic> CreateDeleteCommand(dynamic aggregNode, string entity, XElement schema, dynamic aggreg)
        {
            return new DeleteCommand<dynamic>(aggregNode, entity, schema, aggreg);
        }

        public UpdateCommand<dynamic> CreateUpdateCommand(dynamic aggregNode, string entity, XElement schema, dynamic aggreg)
        {
            return new UpdateCommand<dynamic>(aggregNode, entity, schema, aggreg);
        }

        public UpdateCommandNode<dynamic> CreateUpdateCommandNode(dynamic aggregNode, dynamic origNode, string entity, XElement schema, dynamic aggreg, dynamic original)
        {
            return new UpdateCommandNode<dynamic>(aggregNode, origNode, entity, schema, aggreg, original);
        }

        private static IEnumerable<string> GetDynamicMemberNames(dynamic obj)
        {
            IDynamicMetaObjectProvider provider = obj as IDynamicMetaObjectProvider;
            Expression expression = Expression.Parameter(typeof(object), "$arg0");
            DynamicMetaObject dynamicMetaObject = provider.GetMetaObject(expression);
            IEnumerable<string> result = dynamicMetaObject.GetDynamicMemberNames();
            return result.ToList();
        }

        private static object ChangeType(dynamic value, Type conversionType)
        {
            if (value == null) return null;

            if (conversionType == typeof(string))
            {
                string val = value;
                return val;
            }

            if (string.IsNullOrWhiteSpace(value.ToString())) return null;

            if (conversionType == typeof(DateTime))
            {
                DateTime val = value;
                return val;
            }

            if (conversionType == typeof(bool))
            {
                bool val = value;
                return val;
            }

            if (conversionType == typeof(SByte))
            {
                SByte val = value;
                return val;
            }
            if (conversionType == typeof(Int16))
            {
                Int16 val = value;
                return val;
            }
            if (conversionType == typeof(Int32))
            {
                Int32 val = value;
                return val;
            }
            if (conversionType == typeof(Int64))
            {
                Int64 val = value;
                return val;
            }
            if (conversionType == typeof(Byte))
            {
                Byte val = value;
                return val;
            }
            if (conversionType == typeof(UInt16))
            {
                UInt16 val = value;
                return val;
            }
            if (conversionType == typeof(UInt32))
            {
                UInt32 val = value;
                return val;
            }
            if (conversionType == typeof(UInt64))
            {
                Int16 val = value;
                return val;
            }
            if (conversionType == typeof(Decimal))
            {
                Decimal val = value;
                return val;
            }
            if (conversionType == typeof(Single))
            {
                Single val = value;
                return val;
            }
            if (conversionType == typeof(Double))
            {
                Double val = value;
                return val;
            }

            if (conversionType == typeof(Guid))
            {
                Guid val = value;
                return val;
            }

            if (conversionType == typeof(byte[]))
            {
                string val = value;
                return Convert.FromBase64String(val);
            }

            return value;
        }


    }
}
