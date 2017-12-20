using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using XData.Data.DataObjects;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.OData
{
    public class ODataQuerier<T> : ODataQuerier
    {
        public Database Database { get; private set; }
        protected readonly XElement Schema;
        protected readonly DataConverter<T> DataConverter;

        protected ODataQuerier(Database database, XElement schema, DataConverter<T> conv)
        {
            Database = database;
            Schema = schema;
            DataConverter = conv;
        }

        protected ODataQuerier(Database database, XElement schema, string dataConverter, string dateFormatter, string format)
        {
            Database = database;
            Schema = schema;
            DataConverter = new DataConverterManufacturer().Create<T>(dataConverter);

            DateFormatter formatter;
            if (string.IsNullOrWhiteSpace(dateFormatter))
            {
                if (DataConverter is DataConverter<XElement>)
                {
                    formatter = new DotNETDateFormatter();
                }
                else if (DataConverter is DataConverter<string>)
                {
                    formatter = new JsonNETFormatter() { TimezoneOffset = schema.GetTimezoneOffset() };
                }
                else
                {
                    throw new ArgumentNullException("dateFormatter");
                }
            }
            else
            {
                formatter = new DateFormatterManufacturer().Create(dateFormatter, schema.GetTimezoneOffset(), format);
            }

            DataConverter.DateFormatter = formatter;
        }

        public DateTime GetNow()
        {
            return Database.GetNow();
        }

        public DateTime GetUtcNow()
        {
            return Database.GetUtcNow();
        }

        // overload
        public int Count(string entity, string filter)
        {
            return Count(entity, filter, EmptyParameterValues);
        }

        // overload
        public int Count(string entity, string filter, IEnumerable<KeyValuePair<string, string>> parameterValues)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            return Count(entity, filter, parameterValues, parameters);
        }

        // overload
        public int Count(string entity, string filter, IReadOnlyDictionary<string, object> parameters)
        {
            IEnumerable<KeyValuePair<string, string>> parameterValues = GetParameterValues(parameters);
            return Count(entity, filter, parameterValues, parameters);
        }

        protected int Count(string entity, string filter, IEnumerable<KeyValuePair<string, string>> parameterValues, IReadOnlyDictionary<string, object> parameters)
        {
            ParameterCollection parameterCollection = new ParameterCollection(parameterValues);
            Query query = new Query(entity, null, filter, null, Schema, parameterCollection);
            parameterCollection.ResetParameterValues(parameters);
            return Database.Count(query);
        }

        public T GetDefault(string entity, string select)
        {
            Query query = new Query(entity, select, null, null, Schema, new ParameterCollection(EmptyParameterValues));
            DataTable table = Database.GetDefault(query);
            return DataConverter.Convert(table, entity).First();
        }

        public T GetDefault(string entity, string select, out XElement xsd)
        {
            Query query = new Query(entity, select, null, null, Schema, new ParameterCollection(EmptyParameterValues));
            DataTable table = Database.GetDefault(query);
            xsd = DataConverter.GenerateEntityXsd(table, entity, Schema);
            return DataConverter.Convert(table, entity).First();
        }

        // overload
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby)
        {
            return GetCollection(entity, select, filter, orderby, EmptyParameterValues);
        }

        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, IEnumerable<KeyValuePair<string, string>> parameterValues)
        {
            Query query = new Query(entity, select, filter, orderby, Schema, new ParameterCollection(parameterValues));
            DataTable table = Database.GetCollection(query);
            return DataConverter.Convert(table, entity);
        }

        // overload
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, IReadOnlyDictionary<string, object> parameters)
        {
            IEnumerable<KeyValuePair<string, string>> parameterValues = GetParameterValues(parameters);
            return GetCollection(entity, select, filter, orderby, parameterValues, parameters);
        }

        protected IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, IEnumerable<KeyValuePair<string, string>> parameterValues,
            IReadOnlyDictionary<string, object> parameters)
        {
            ParameterCollection parameterCollection = new ParameterCollection(parameterValues);
            Query query = new Query(entity, select, filter, orderby, Schema, parameterCollection);
            parameterCollection.ResetParameterValues(parameters);
            DataTable table = Database.GetCollection(query);
            return DataConverter.Convert(table, entity);
        }

        // overload
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, out XElement xsd)
        {
            return GetCollection(entity, select, filter, orderby, EmptyParameterValues, out xsd);
        }

        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, IEnumerable<KeyValuePair<string, string>> parameterValues, out XElement xsd)
        {
            Query query = new Query(entity, select, filter, orderby, Schema, new ParameterCollection(parameterValues));
            DataTable table = Database.GetCollection(query);
            xsd = DataConverter.GenerateCollectionXsd(table, entity, Schema);
            return DataConverter.Convert(table, entity);
        }

        // overload
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, IReadOnlyDictionary<string, object> parameters, out XElement xsd)
        {
            IEnumerable<KeyValuePair<string, string>> parameterValues = GetParameterValues(parameters);
            return GetCollection(entity, select, filter, orderby, parameterValues, parameters, out xsd);
        }

        protected IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, IEnumerable<KeyValuePair<string, string>> parameterValues,
            IReadOnlyDictionary<string, object> parameters, out XElement xsd)
        {
            ParameterCollection parameterCollection = new ParameterCollection(parameterValues);
            Query query = new Query(entity, select, filter, orderby, Schema, parameterCollection);
            parameterCollection.ResetParameterValues(parameters);
            DataTable table = Database.GetCollection(query);
            xsd = DataConverter.GenerateCollectionXsd(table, entity, Schema);
            return DataConverter.Convert(table, entity);
        }

        // overload
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top)
        {
            return GetPagingCollection(entity, select, filter, orderby, skip, top, EmptyParameterValues);
        }

        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top,
            IEnumerable<KeyValuePair<string, string>> parameterValues)
        {
            Query query = new Query(entity, select, filter, orderby, skip, top, Schema, new ParameterCollection(parameterValues));
            DataTable table = Database.GetCollection(query);
            return DataConverter.Convert(table, entity);
        }

        // overload
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top,
           IReadOnlyDictionary<string, object> parameters)
        {
            IEnumerable<KeyValuePair<string, string>> parameterValues = GetParameterValues(parameters);
            return GetPagingCollection(entity, select, filter, orderby, skip, top, parameterValues, parameters);
        }

        protected IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top,
            IEnumerable<KeyValuePair<string, string>> parameterValues, IReadOnlyDictionary<string, object> parameters)
        {
            ParameterCollection parameterCollection = new ParameterCollection(parameterValues);
            Query query = new Query(entity, select, filter, orderby, skip, top, Schema, parameterCollection);
            parameterCollection.ResetParameterValues(parameters);
            DataTable table = Database.GetCollection(query);
            return DataConverter.Convert(table, entity);
        }

        // overload
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, out XElement xsd)
        {
            return GetPagingCollection(entity, select, filter, orderby, skip, top, EmptyParameterValues, out xsd);
        }

        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top,
            IEnumerable<KeyValuePair<string, string>> parameterValues, out XElement xsd)
        {
            Query query = new Query(entity, select, filter, orderby, skip, top, Schema, new ParameterCollection(parameterValues));
            DataTable table = Database.GetCollection(query);
            xsd = DataConverter.GenerateCollectionXsd(table, entity, Schema);
            return DataConverter.Convert(table, entity);
        }

        // overload
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top,
            IReadOnlyDictionary<string, object> parameters, out XElement xsd)
        {
            IEnumerable<KeyValuePair<string, string>> parameterValues = GetParameterValues(parameters);
            return GetPagingCollection(entity, select, filter, orderby, skip, top, parameterValues, parameters, out xsd);
        }

        protected IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top,
            IEnumerable<KeyValuePair<string, string>> parameterValues, IReadOnlyDictionary<string, object> parameters, out XElement xsd)
        {
            ParameterCollection parameterCollection = new ParameterCollection(parameterValues);
            Query query = new Query(entity, select, filter, orderby, skip, top, Schema, parameterCollection);
            parameterCollection.ResetParameterValues(parameters);
            DataTable table = Database.GetCollection(query);
            xsd = DataConverter.GenerateCollectionXsd(table, entity, Schema);
            return DataConverter.Convert(table, entity);
        }

        public T Find(string entity, string[] key, string select)
        {
            string filter = GetFilter(entity, key);
            return GetCollection(entity, select, filter, null).FirstOrDefault();
        }

        public T Find(string entity, string[] key, string select, out XElement xsd)
        {
            string filter = GetFilter(entity, key);

            Query query = new Query(entity, select, filter, null, Schema, new ParameterCollection(EmptyParameterValues));
            DataTable table = Database.GetCollection(query);
            xsd = DataConverter.GenerateEntityXsd(table, entity, Schema);
            return DataConverter.Convert(table, entity).FirstOrDefault();
        }

        protected string GetFilter(string entity, string[] key)
        {
            string[] keyPropertyNames = Schema.GetKeySchema(entity).Elements().Select(x => x.Attribute(SchemaVocab.Name).Value).ToArray();
            List<string> list = new List<string>();
            for (int i = 0; i < keyPropertyNames.Length; i++)
            {
                string name = keyPropertyNames[i];
                string value = key[i];
                list.Add(string.Format("{0} eq {1}", name, value));
            }
            return string.Join(" and ", list);
        }

        // overload
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, string expand)
        {
            return GetCollection(entity, select, filter, orderby, expand, EmptyParameterValues);
        }

        // expand
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, string expand, IEnumerable<KeyValuePair<string, string>> parameterValues)
        {
            Query query = new Query(entity, select, filter, orderby, Schema, new ParameterCollection(parameterValues));
            QueryExpand queryExpand = new QueryExpand(query, expand);
            ResultNode resultNode = Database.GetCollection(queryExpand);
            return DataConverter.Convert(resultNode);
        }

        // overload
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, string expand,
            IReadOnlyDictionary<string, object> parameters)
        {
            IEnumerable<KeyValuePair<string, string>> parameterValues = GetParameterValues(parameters);
            return GetCollection(entity, select, filter, orderby, expand, parameterValues, parameters);
        }

        // expand
        protected IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, string expand,
            IEnumerable<KeyValuePair<string, string>> parameterValues, IReadOnlyDictionary<string, object> parameters)
        {
            ParameterCollection parameterCollection = new ParameterCollection(parameterValues);
            Query query = new Query(entity, select, filter, orderby, Schema, parameterCollection);
            QueryExpand queryExpand = new QueryExpand(query, expand);
            parameterCollection.ResetParameterValues(parameters);
            ResultNode resultNode = Database.GetCollection(queryExpand);
            return DataConverter.Convert(resultNode);
        }

        // overload
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, string expand)
        {
            return GetPagingCollection(entity, select, filter, orderby, skip, top, expand, EmptyParameterValues);
        }

        // expand
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, string expand,
            IEnumerable<KeyValuePair<string, string>> parameterValues)
        {
            Query query = new Query(entity, select, filter, orderby, skip, top, Schema, new ParameterCollection(parameterValues));
            QueryExpand queryExpand = new QueryExpand(query, expand);
            ResultNode resultNode = Database.GetCollection(queryExpand);
            return DataConverter.Convert(resultNode);
        }

        // overload
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, string expand,
            IReadOnlyDictionary<string, object> parameters)
        {
            IEnumerable<KeyValuePair<string, string>> parameterValues = GetParameterValues(parameters);
            return GetPagingCollection(entity, select, filter, orderby, skip, top, expand, parameterValues, parameters);
        }

        // expand
        protected IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, string expand,
            IEnumerable<KeyValuePair<string, string>> parameterValues, IReadOnlyDictionary<string, object> parameters)
        {
            ParameterCollection parameterCollection = new ParameterCollection(parameterValues);
            Query query = new Query(entity, select, filter, orderby, skip, top, Schema, parameterCollection);
            QueryExpand queryExpand = new QueryExpand(query, expand);
            parameterCollection.ResetParameterValues(parameters);
            ResultNode resultNode = Database.GetCollection(queryExpand);
            return DataConverter.Convert(resultNode);
        }

        // overload
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, string expand, out XElement xsd)
        {
            return GetCollection(entity, select, filter, orderby, expand, EmptyParameterValues, out xsd);
        }

        // expand
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, string expand,
            IEnumerable<KeyValuePair<string, string>> parameterValues, out XElement xsd)
        {
            Query query = new Query(entity, select, filter, orderby, Schema, new ParameterCollection(parameterValues));
            QueryExpand queryExpand = new QueryExpand(query, expand);
            ResultNode resultNode = Database.GetCollection(queryExpand);

            xsd = DataConverter.GenerateCollectionXsd(resultNode, Schema);
            return DataConverter.Convert(resultNode);
        }

        // overload
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, string expand,
            IReadOnlyDictionary<string, object> parameters, out XElement xsd)
        {
            IEnumerable<KeyValuePair<string, string>> parameterValues = GetParameterValues(parameters);
            return GetCollection(entity, select, filter, orderby, expand, parameterValues, parameters, out xsd);
        }

        // expand
        protected IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, string expand,
            IEnumerable<KeyValuePair<string, string>> parameterValues, IReadOnlyDictionary<string, object> parameters, out XElement xsd)
        {
            ParameterCollection parameterCollection = new ParameterCollection(parameterValues);
            Query query = new Query(entity, select, filter, orderby, Schema, parameterCollection);
            QueryExpand queryExpand = new QueryExpand(query, expand);
            parameterCollection.ResetParameterValues(parameters);
            ResultNode resultNode = Database.GetCollection(queryExpand);

            xsd = DataConverter.GenerateCollectionXsd(resultNode, Schema);
            return DataConverter.Convert(resultNode);
        }

        // overload
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, string expand, out XElement xsd)
        {
            return GetPagingCollection(entity, select, filter, orderby, skip, top, expand, EmptyParameterValues, out xsd);
        }

        // expand
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, string expand,
            IEnumerable<KeyValuePair<string, string>> parameterValues, out XElement xsd)
        {
            Query query = new Query(entity, select, filter, orderby, skip, top, Schema, new ParameterCollection(parameterValues));
            QueryExpand queryExpand = new QueryExpand(query, expand);
            ResultNode resultNode = Database.GetCollection(queryExpand);

            xsd = DataConverter.GenerateCollectionXsd(resultNode, Schema);
            return DataConverter.Convert(resultNode);
        }

        // overload
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, string expand,
            IReadOnlyDictionary<string, object> parameters, out XElement xsd)
        {
            IEnumerable<KeyValuePair<string, string>> parameterValues = GetParameterValues(parameters);
            return GetPagingCollection(entity, select, filter, orderby, skip, top, expand, parameterValues, parameters, out xsd);
        }

        // expand
        protected IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, string expand,
            IEnumerable<KeyValuePair<string, string>> parameterValues, IReadOnlyDictionary<string, object> parameters, out XElement xsd)
        {
            ParameterCollection parameterCollection = new ParameterCollection(parameterValues);
            Query query = new Query(entity, select, filter, orderby, skip, top, Schema, parameterCollection);
            QueryExpand queryExpand = new QueryExpand(query, expand);
            parameterCollection.ResetParameterValues(parameters);
            ResultNode resultNode = Database.GetCollection(queryExpand);

            xsd = DataConverter.GenerateCollectionXsd(resultNode, Schema);
            return DataConverter.Convert(resultNode);
        }

        // expand
        public T Find(string entity, string[] key, string select, string expand)
        {
            string filter = GetFilter(entity, key);
            return GetCollection(entity, select, filter, null, expand).FirstOrDefault();
        }

        // expand
        public T Find(string entity, string[] key, string select, string expand, out XElement xsd)
        {
            string filter = GetFilter(entity, key);

            Query query = new Query(entity, select, filter, null, Schema, new ParameterCollection(EmptyParameterValues));
            QueryExpand queryExpand = new QueryExpand(query, expand);
            ResultNode resultNode = Database.GetCollection(queryExpand);

            xsd = DataConverter.GenerateEntityXsd(resultNode, Schema);
            return DataConverter.Convert(resultNode).FirstOrDefault();
        }

        // overload
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, Expand[] expands)
        {
            return GetCollection(entity, select, filter, orderby, expands, EmptyParameterValues);
        }

        // expand
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, Expand[] expands, IEnumerable<KeyValuePair<string, string>> parameterValues)
        {
            Query query = new Query(entity, select, filter, orderby, Schema, new ParameterCollection(parameterValues));
            QueryExpand queryExpand = new QueryExpand(query, expands);
            ResultNode resultNode = Database.GetCollection(queryExpand);
            return DataConverter.Convert(resultNode);
        }

        // overload
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, Expand[] expands,
            IReadOnlyDictionary<string, object> parameters)
        {
            IEnumerable<KeyValuePair<string, string>> parameterValues = GetParameterValues(parameters);
            return GetCollection(entity, select, filter, orderby, expands, parameterValues, parameters);
        }

        // expand
        protected IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, Expand[] expands,
            IEnumerable<KeyValuePair<string, string>> parameterValues, IReadOnlyDictionary<string, object> parameters)
        {
            ParameterCollection parameterCollection = new ParameterCollection(parameterValues);
            Query query = new Query(entity, select, filter, orderby, Schema, parameterCollection);
            QueryExpand queryExpand = new QueryExpand(query, expands);
            parameterCollection.ResetParameterValues(parameters);
            ResultNode resultNode = Database.GetCollection(queryExpand);
            return DataConverter.Convert(resultNode);
        }

        // overload
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, Expand[] expands)
        {
            return GetPagingCollection(entity, select, filter, orderby, skip, top, expands, EmptyParameterValues);
        }

        // expand
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, Expand[] expands,
            IEnumerable<KeyValuePair<string, string>> parameterValues)
        {
            Query query = new Query(entity, select, filter, orderby, skip, top, Schema, new ParameterCollection(parameterValues));
            QueryExpand queryExpand = new QueryExpand(query, expands);
            ResultNode resultNode = Database.GetCollection(queryExpand);
            return DataConverter.Convert(resultNode);
        }

        // overload
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, Expand[] expands,
            IReadOnlyDictionary<string, object> parameters)
        {
            IEnumerable<KeyValuePair<string, string>> parameterValues = GetParameterValues(parameters);
            return GetPagingCollection(entity, select, filter, orderby, skip, top, expands, parameterValues, parameters);
        }

        // expand
        protected IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, Expand[] expands,
            IEnumerable<KeyValuePair<string, string>> parameterValues, IReadOnlyDictionary<string, object> parameters)
        {
            ParameterCollection parameterCollection = new ParameterCollection(parameterValues);
            Query query = new Query(entity, select, filter, orderby, skip, top, Schema, parameterCollection);
            QueryExpand queryExpand = new QueryExpand(query, expands);
            parameterCollection.ResetParameterValues(parameters);
            ResultNode resultNode = Database.GetCollection(queryExpand);
            return DataConverter.Convert(resultNode);
        }

        // overload
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, Expand[] expands, out XElement xsd)
        {
            return GetCollection(entity, select, filter, orderby, expands, EmptyParameterValues, out xsd);
        }

        // expand
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, Expand[] expands,
            IEnumerable<KeyValuePair<string, string>> parameterValues, out XElement xsd)
        {
            Query query = new Query(entity, select, filter, orderby, Schema, new ParameterCollection(parameterValues));
            QueryExpand queryExpand = new QueryExpand(query, expands);
            ResultNode resultNode = Database.GetCollection(queryExpand);

            xsd = DataConverter.GenerateCollectionXsd(resultNode, Schema);
            return DataConverter.Convert(resultNode);
        }

        // overload
        public IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, Expand[] expands,
            IReadOnlyDictionary<string, object> parameters, out XElement xsd)
        {
            IEnumerable<KeyValuePair<string, string>> parameterValues = GetParameterValues(parameters);
            return GetCollection(entity, select, filter, orderby, expands, parameterValues, parameters, out xsd);
        }

        // expand
        protected IEnumerable<T> GetCollection(string entity, string select, string filter, string orderby, Expand[] expands,
            IEnumerable<KeyValuePair<string, string>> parameterValues, IReadOnlyDictionary<string, object> parameters, out XElement xsd)
        {
            ParameterCollection parameterCollection = new ParameterCollection(parameterValues);
            Query query = new Query(entity, select, filter, orderby, Schema, parameterCollection);
            QueryExpand queryExpand = new QueryExpand(query, expands);
            parameterCollection.ResetParameterValues(parameters);
            ResultNode resultNode = Database.GetCollection(queryExpand);

            xsd = DataConverter.GenerateCollectionXsd(resultNode, Schema);
            return DataConverter.Convert(resultNode);
        }

        // overload
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, Expand[] expands, out XElement xsd)
        {
            return GetPagingCollection(entity, select, filter, orderby, skip, top, expands, EmptyParameterValues, out xsd);
        }

        // expand
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, Expand[] expands,
            IEnumerable<KeyValuePair<string, string>> parameterValues, out XElement xsd)
        {
            Query query = new Query(entity, select, filter, orderby, skip, top, Schema, new ParameterCollection(parameterValues));
            QueryExpand queryExpand = new QueryExpand(query, expands);
            ResultNode resultNode = Database.GetCollection(queryExpand);

            xsd = DataConverter.GenerateCollectionXsd(resultNode, Schema);
            return DataConverter.Convert(resultNode);
        }

        // overload
        public IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, Expand[] expands,
            IReadOnlyDictionary<string, object> parameters, out XElement xsd)
        {
            IEnumerable<KeyValuePair<string, string>> parameterValues = GetParameterValues(parameters);
            return GetPagingCollection(entity, select, filter, orderby, skip, top, expands, parameterValues, parameters, out xsd);
        }

        // expand
        protected IEnumerable<T> GetPagingCollection(string entity, string select, string filter, string orderby, long skip, long top, Expand[] expands,
            IEnumerable<KeyValuePair<string, string>> parameterValues, IReadOnlyDictionary<string, object> parameters, out XElement xsd)
        {
            ParameterCollection parameterCollection = new ParameterCollection(parameterValues);
            Query query = new Query(entity, select, filter, orderby, skip, top, Schema, parameterCollection);
            QueryExpand queryExpand = new QueryExpand(query, expands);
            parameterCollection.ResetParameterValues(parameters);
            ResultNode resultNode = Database.GetCollection(queryExpand);

            xsd = DataConverter.GenerateCollectionXsd(resultNode, Schema);
            return DataConverter.Convert(resultNode);
        }

        // expand
        public T Find(string entity, string[] key, string select, Expand[] expands)
        {
            string filter = GetFilter(entity, key);
            return GetCollection(entity, select, filter, null, expands).FirstOrDefault();
        }

        // expand
        public T Find(string entity, string[] key, string select, Expand[] expands, out XElement xsd)
        {
            string filter = GetFilter(entity, key);

            Query query = new Query(entity, select, filter, null, Schema, new ParameterCollection(EmptyParameterValues));
            QueryExpand queryExpand = new QueryExpand(query, expands);
            ResultNode resultNode = Database.GetCollection(queryExpand);

            xsd = DataConverter.GenerateEntityXsd(resultNode, Schema);
            return DataConverter.Convert(resultNode).FirstOrDefault();
        }

        protected static IEnumerable<KeyValuePair<string, string>> GetParameterValues(IReadOnlyDictionary<string, object> parameters)
        {
            List<KeyValuePair<string, string>> parameterValues = new List<KeyValuePair<string, string>>();
            foreach (KeyValuePair<string, object> pair in parameters)
            {
                parameterValues.Add(new KeyValuePair<string, string>(pair.Key, null));
            }
            return parameterValues;
        }

        public static ODataQuerier<T> Create(string name, XElement schema = null, string dataConverter = null, string dateFormatter = null, string format = null)
        {
            XElement xSchema = schema ?? new PrimarySchemaProvider().GetSchema(name);

            string sConverter;
            if (string.IsNullOrWhiteSpace(dataConverter))
            {
                if (typeof(T) == typeof(XElement))
                {
                    sConverter = "xml";
                }
                else if (typeof(T) == typeof(string))
                {
                    sConverter = "json";
                }
                else
                {
                    throw new NotSupportedException(typeof(T).ToString());
                }
            }
            else
            {
                sConverter = dataConverter;
            }
            Database database = new DatabaseManufacturer().Create(name);
            return new ODataQuerier<T>(database, xSchema, sConverter, dateFormatter, format);
        }


    }
}
