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

        public static int Count(string name, XElement schema, string entity, string filter, IEnumerable<KeyValuePair<string, string>> parameterValues)
        {
            return Count(name, schema, entity, filter, (object)parameterValues);
        }

        public static int Count(string name, XElement schema, string entity, string filter, IReadOnlyDictionary<string, object> parameterValues)
        {
            return Count(name, schema, entity, filter, (object)parameterValues);
        }

        protected static int Count(string name, XElement schema, string entity, string filter, object parameterValues)
        {
            ParameterCollection parameterCollection = new ParameterCollection();
            Query query = new Query(entity, null, filter, null, schema, parameterCollection);
            SetParameterValues(parameterCollection, parameterValues);

            Database database = new DatabaseManufacturer().Create(name);
            return database.Count(query);
        }

        protected static void SetParameterValues(ParameterCollection parameterCollection, object parameterValues)
        {
            if (parameterValues is IEnumerable<KeyValuePair<string, string>>)
            {
                parameterCollection.SetParameterValues((IEnumerable<KeyValuePair<string, string>>)parameterValues);
            }
            if (parameterValues is IReadOnlyDictionary<string, object>)
            {
                parameterCollection.SetParameterValues((IReadOnlyDictionary<string, object>)parameterValues);
            }
        }


    }
}
