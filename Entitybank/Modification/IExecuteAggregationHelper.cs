using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XData.Data.Modification
{
    public interface IExecuteAggregationHelper<T>
    {
        IEnumerable<T> GetChildren(T obj);
        void ResetObjectValues(T obj, Dictionary<string, object> propertyValues);
        Dictionary<string, object> GetPropertyValues(T obj, XElement entitySchema);
        Dictionary<XElement, T> GetPropertySchemaChildrenDictionary(T obj, XElement entitySchema);
    }

    public interface ICreateAggregationHelper<T> : IExecuteAggregationHelper<T>
    {
        InsertCommand<T> CreateInsertCommand(T aggregNode, string entity, XElement schema, T aggreg);
    }

    public interface IDeleteAggregationHelper<T> : IExecuteAggregationHelper<T>
    {
        DeleteCommand<T> CreateDeleteCommand(T aggregNode, string entity, XElement schema, T aggreg);
        UpdateCommand<T> CreateUpdateCommand(T aggregNode, string entity, XElement schema, T aggreg);
    }

    public interface IUpdateAggregationHelper<T> : IExecuteAggregationHelper<T>
    {
        UpdateCommandNode<T> CreateUpdateCommandNode(T aggregNode, T origNode, string entity, XElement schema, T aggreg, T original);
    }


}
