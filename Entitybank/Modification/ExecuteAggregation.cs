using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Modification
{
    public abstract partial class ExecuteAggregation<T>
    {
        public T Aggreg { get; private set; }
        public string Entity { get; private set; }
        public XElement Schema { get; private set; }

        protected List<ExecuteCommand<T>> Commands = new List<ExecuteCommand<T>>();
        internal IEnumerable<ExecuteCommand<T>> ExecuteCommands { get => Commands; }

        protected IExecuteAggregationHelper<T> ExecuteAggregationHelper { get; private set; }

        public ExecuteAggregation(T aggreg, string entity, XElement schema)
        {
            Aggreg = aggreg;
            Entity = entity;
            Schema = schema;

            ExecuteAggregationHelper = GetExecuteAggregationHelper();
        }

        protected IEnumerable<T> GetChildren(T obj)
        {
            return ExecuteAggregationHelper.GetChildren(obj);
        }

        protected void ResetObjectValues(T obj, Dictionary<string, object> propertyValues)
        {
            ExecuteAggregationHelper.ResetObjectValues(obj, propertyValues);
        }

        protected Dictionary<string, object> GetPropertyValues(T obj, XElement entitySchema)
        {
            return ExecuteAggregationHelper.GetPropertyValues(obj, entitySchema);
        }

        protected IEnumerable<KeyValuePair<XElement, T>> GetPropertySchemaChildrens(T obj, XElement entitySchema)
        {
            return ExecuteAggregationHelper.GetPropertySchemaChildrens(obj, entitySchema);
        }

        protected abstract IExecuteAggregationHelper<T> GetExecuteAggregationHelper();
    }
}
