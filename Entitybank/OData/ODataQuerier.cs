using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.OData
{
    public class ODataQuerier
    {
        public static DateTime GetNow(string name)
        {
            Database database = new DatabaseManufacturer().Create(name);
            return database.GetNow();
        }

        public static DateTime GetUtcNow(string name)
        {
            Database database = new DatabaseManufacturer().Create(name);
            return database.GetUtcNow();
        }

        protected static readonly IEnumerable<KeyValuePair<string, string>> EmptyParameterValues = new List<KeyValuePair<string, string>>();

        // overload
        public static int Count(string name, XElement schema, string entity, string filter)
        {
            return Count(name, schema, entity, filter, EmptyParameterValues);
        }

        // overload
        public static int Count(string name, XElement schema, string entity, string filter, IEnumerable<KeyValuePair<string, string>> parameterValues)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            return Count(name, schema, entity, filter, parameterValues, parameters);
        }

        public static int Count(string name, XElement schema, string entity, string filter,
            IEnumerable<KeyValuePair<string, string>> parameterValues, IReadOnlyDictionary<string, object> parameters)
        {
            ParameterCollection parameterCollection = new ParameterCollection(parameterValues);
            parameterCollection.ResetParameterValues(parameters);
            Query query = new Query(entity, null, filter, null, schema, parameterCollection);
            Database database = new DatabaseManufacturer().Create(name);
            return database.Count(query);
        }


    }
}
