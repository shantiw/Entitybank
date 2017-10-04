using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.OData;

namespace XData.Data.Services
{
    // name: ConnectionStringName
    public sealed class ODataService<T> : ODataService
    {
        private readonly string _name;
        private readonly IEnumerable<KeyValuePair<string, string>> _keyValues;
        private readonly string _conv;

        private readonly XElement _schema;
        public XElement Schema { get => _schema; }

        public ODataService(string name, IEnumerable<KeyValuePair<string, string>> keyValues, string conv)
        {
            _name = name;
            _keyValues = keyValues;
            _conv = conv;

            _schema = GetSchema(_name, GetDeltaSchemaKey(_keyValues));
        }

        public T GetDefault(string entity)
        {
            string select = GetValue("$select");
            string dateFormatter = GetDateFormatter(out string format);
            ODataQuerier<T> querier = ODataQuerier<T>.Create(_name, _schema, GetConv(), dateFormatter, format);
            return querier.GetDefault(entity, select);
        }

        public T GetDefault(string entity, out XElement xsd)
        {
            string select = GetValue("$select");
            string xsdVal = GetValue("xsd") ?? "false";

            string dateFormatter = GetDateFormatter(out string format);
            ODataQuerier<T> querier = ODataQuerier<T>.Create(_name, _schema, GetConv(), dateFormatter, format);
            if (xsdVal == "true" || xsdVal == "xsd")
            {
                return querier.GetDefault(entity, select, out xsd);
            }
            else
            {
                xsd = null;
                return querier.GetDefault(entity, select);
            }
        }

        // overload
        public IEnumerable<T> GetCollection(string collection)
        {
            return GetCollection(collection, false, out XElement xsd);
        }

        public IEnumerable<T> GetCollection(string collection, out XElement xsd)
        {
            string xsdVal = GetValue("xsd") ?? "false";
            return GetCollection(collection, xsdVal == "true" || xsdVal == "xsd", out xsd);
        }

        private IEnumerable<T> GetCollection(string collection, bool outXsd, out XElement xsd)
        {
            string entity = GetEntity(collection);

            string select = GetValue("$select");
            string filter = GetValue("$filter");
            string orderby = GetValue("$orderby");

            string sSkip = GetValue("$skip");
            string sTop = GetValue("$top");
            long skip = (sSkip == null) ? 0 : long.Parse(sSkip);
            long top = (sTop == null) ? 0 : long.Parse(sTop);

            string expand = GetValue("$expand");

            IEnumerable<KeyValuePair<string, string>> parameterValues = GetParameterValues();

            string dateFormatter = GetDateFormatter(out string format);
            ODataQuerier<T> querier = ODataQuerier<T>.Create(_name, _schema, GetConv(), dateFormatter, format);

            IEnumerable<T> result;
            if (outXsd)
            {
                if (expand == null)
                {
                    result = (skip == 0 && top == 0)
                        ? querier.GetCollection(entity, select, filter, orderby, parameterValues, out xsd)
                        : querier.GetCollection(entity, select, filter, orderby, skip, top, parameterValues, out xsd);
                }
                else
                {
                    result = (skip == 0 && top == 0)
                        ? querier.GetCollection(entity, select, filter, orderby, expand, parameterValues, out xsd)
                        : querier.GetCollection(entity, select, filter, orderby, skip, top, expand, parameterValues, out xsd);
                }
            }
            else
            {
                xsd = null;

                if (expand == null)
                {
                    result = (skip == 0 && top == 0)
                        ? querier.GetCollection(entity, select, filter, orderby, parameterValues)
                        : querier.GetCollection(entity, select, filter, orderby, skip, top, parameterValues);
                }
                else
                {
                    result = (skip == 0 && top == 0)
                        ? querier.GetCollection(entity, select, filter, orderby, expand, parameterValues)
                        : querier.GetCollection(entity, select, filter, orderby, skip, top, expand, parameterValues);
                }
            }

            return result;
        }

        // overload
        public T Find(string collection, string[] key)
        {
            return Find(collection, key, false, out XElement xsd);
        }

        public T Find(string collection, string[] key, out XElement xsd)
        {
            string xsdVal = GetValue("xsd") ?? "false";
            return Find(collection, key, xsdVal == "true" || xsdVal == "xsd", out xsd);
        }

        private T Find(string collection, string[] key, bool outXsd, out XElement xsd)
        {
            string entity = GetEntity(collection);

            string select = GetValue("$select");
            string expand = GetValue("$expand");

            string dateFormatter = GetDateFormatter(out string format);
            ODataQuerier<T> querier = ODataQuerier<T>.Create(_name, _schema, GetConv(), dateFormatter, format);

            T result;
            if (outXsd)
            {
                if (expand == null)
                {
                    result = querier.Find(entity, key, select, out xsd);
                }
                else
                {
                    result = querier.Find(entity, key, select, expand, out xsd);
                }
            }
            else
            {
                xsd = null;

                if (expand == null)
                {
                    result = querier.Find(entity, key, select);
                }
                else
                {
                    result = querier.Find(entity, key, select, expand);
                }
            }

            return result;
        }

        public string GetEntity(string collection)
        {
            return GetEntity(_schema, collection);
        }

        private IEnumerable<KeyValuePair<string, string>> GetParameterValues()
        {
            return GetParameterValues(_keyValues);
        }

        private string GetValue(string key)
        {
            return GetValue(_keyValues, key);
        }

        private string GetConv()
        {
            string conv = GetValue("conv");
            return string.IsNullOrWhiteSpace(conv) ? _conv : conv;
        }

        private string GetDateFormatter(out string format)
        {
            string dateFormatter = GetValue("date");
            if (string.IsNullOrWhiteSpace(dateFormatter))
            {
                format = null;
                return null;
            }
            string[] ss = dateFormatter.Split(',');
            if (ss.Length == 1)
            {
                format = null;
                return ss[0].Trim();
            }
            else
            {
                format = ss[1].Trim();
                if (format.StartsWith("'") && format.EndsWith("'"))
                {
                    format = format.Substring(1, format.Length - 2);
                    format = format.Replace("''", "'");
                }
                return ss[0].Trim();
            }
        }


    }
}
