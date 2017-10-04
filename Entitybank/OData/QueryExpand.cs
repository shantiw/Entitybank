using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using XData.Data.Helpers;
using XData.Data.Schema;

namespace XData.Data.OData
{
    public class QueryExpand
    {
        // {e1e0601c-7183-482f-955d-dfb8e36a194c}
        protected const string GuidPattern = AnalysisHelper.GuidPattern;

        // outermost (...)
        protected const string ParenthesesPairPattern = AnalysisHelper.ParenthesesPairPattern;

        public Query Query { get; private set; }
        public XElement Schema { get; private set; }
        public ParameterCollection ParameterCollection { get; private set; }
        public ExpandNode[] Nodes { get; private set; }

        protected readonly Dictionary<string, string> StringPlaceholders;

        // Trips($filter=contains(Name, 'Holiday') $select=Id,Name $orderby=Id desc $expand=Hotels),Contacts($filter=Name eq 'John')
        public QueryExpand(Query query, string expand)
        {
            Query = query;
            Schema = new XElement(query.Schema);
            ParameterCollection = query.ParameterCollection;

            string value = EncodeString(expand, out StringPlaceholders);
            XElement entitySchema = Schema.GetEntitySchema(Query.Entity);
            string collection = entitySchema.Attribute(SchemaVocab.Collection).Value;
            Nodes = Compose(value, entitySchema, collection);
        }

        // Trips($filter=contains(Name, {f5eac763-e025-4cf8-aa1d-9bb3a2986515}) $select=Id,Name $orderby=Id desc $expand=Hotels),Contacts($filter=Name eq {670bc07f-6b33-47fb-be01-63834e25ff21})
        protected ExpandNode[] Compose(string value, XElement parentSchema, string parentPath)
        {
            List<ExpandNode> expands = new List<ExpandNode>();

            string val = value;

            //
            Dictionary<string, string> placeholders = new Dictionary<string, string>();
            val = Regex.Replace(val, ParenthesesPairPattern, new MatchEvaluator(m =>
            {
                string guid = GetGuid();
                placeholders.Add(guid, m.Value);
                return guid;
            }));

            //
            string[] expandStrings = val.Split(',');
            for (int i = 0; i < expandStrings.Length; i++)
            {
                string expandString = expandStrings[i].Trim();
                expandString = DecodeString(expandString, placeholders);

                string property = GetProperty(expandString, out string select, out string filter, out string orderby, out string expand);

                XElement[] propertyPath = Schema.GenerateExpandPropertyPath(parentSchema, property);

                ExpandProperty oProperty = ExpandProperty.Create(property, propertyPath, parentSchema, Schema);

                string path = parentPath + "/" + property;
                ExpandNode oExpand = ExpandNode.Create(oProperty, select, filter, orderby, Schema, ParameterCollection);
                oExpand.Path = path;
                expands.Add(oExpand);

                //
                if (!string.IsNullOrWhiteSpace(expand))
                {
                    oExpand.Children = Compose(expand, propertyPath[propertyPath.Length - 1], path);
                }
            }

            return expands.ToArray();
        }

        // Trips($filter=contains(Name, {f5eac763-e025-4cf8-aa1d-9bb3a2986515}) $select=Id,Name $orderby=Id,Name $expand=Hotels($expand=Addrs($select=Id,Name $expand=Details),Units))
        protected string GetProperty(string expandString, out string select, out string filter, out string orderby, out string expand)
        {
            select = null;
            filter = null;
            orderby = null;
            expand = null;

            int index = expandString.IndexOf('(');
            if (index == -1) return expandString;

            string property = expandString.Substring(0, index).Trim();
            string val = expandString.Substring(index + 1).Trim();
            val = val.Substring(0, val.Length - 1);

            //
            Dictionary<string, string> placeholders = new Dictionary<string, string>();
            val = Regex.Replace(val, ParenthesesPairPattern, new MatchEvaluator(m =>
            {
                string guid = GetGuid();
                placeholders.Add(guid, m.Value);
                return guid;
            }));

            Dictionary<string, KeyValuePair<int, int>> dict = new Dictionary<string, KeyValuePair<int, int>>();
            string pattern = @"\$(select|filter|orderby|expand)\s*=\s*";
            MatchCollection matches = Regex.Matches(val, pattern);
            foreach (Match match in matches)
            {
                // trim $, =, white space
                string s = match.Value.Trim();
                s = s.Substring(1, s.Length - 2).Trim();

                dict.Add(s, new KeyValuePair<int, int>(match.Index, match.Length));
            }

            Dictionary<string, string> items = new Dictionary<string, string>();
            KeyValuePair<string, KeyValuePair<int, int>>[] array = dict.ToArray();
            for (int i = 0; i < array.Length - 1; i++)
            {
                int start = array[i].Value.Key + array[i].Value.Value;
                int end = array[i + 1].Value.Key;
                string item = val.Substring(start, end - start).Trim();
                items.Add(array[i].Key, item);
            }
            items.Add(array[array.Length - 1].Key, val.Substring(array[array.Length - 1].Value.Key + array[array.Length - 1].Value.Value));

            foreach (KeyValuePair<string, string> pair in items)
            {
                if (pair.Key == "select")
                {
                    select = DecodeString(DecodeString(pair.Value, placeholders));
                }
                else if (pair.Key == "filter")
                {
                    filter = DecodeString(DecodeString(pair.Value, placeholders));
                }
                else if (pair.Key == "orderby")
                {
                    orderby = DecodeString(DecodeString(pair.Value, placeholders));
                }
                else if (pair.Key == "expand")
                {
                    expand = DecodeString(DecodeString(pair.Value, placeholders));
                }
            }

            return property;
        }

        protected string DecodeString(string value)
        {
            return DecodeString(value, StringPlaceholders);
        }

        protected static string EncodeString(string value, out Dictionary<string, string> placeholders)
        {
            return AnalysisHelper.EncodeString(value, out placeholders);
        }

        protected static string DecodeString(string value, Dictionary<string, string> placeholders)
        {
            return AnalysisHelper.DecodeString(value, placeholders);
        }

        protected static string GetGuid()
        {
            return AnalysisHelper.GetGuid();
        }

        public QueryExpand(Query query, Expand[] expands)
        {
            Query = query;
            Schema = new XElement(query.Schema);
            ParameterCollection = query.ParameterCollection;

            XElement entitySchema = Schema.GetEntitySchema(Query.Entity);
            string collection = entitySchema.Attribute(SchemaVocab.Collection).Value;

            Nodes = new ExpandNode[expands.Length];
            for (int i = 0; i < expands.Length; i++)
            {
                Nodes[i] = Compose(expands[i], entitySchema, collection);
            }
        }

        protected ExpandNode Compose(Expand expand, XElement parentSchema, string parentPath)
        {
            string property = expand.Property;

            XElement[] propertyPath = Schema.GenerateExpandPropertyPath(parentSchema, property);

            ExpandProperty oProperty = ExpandProperty.Create(property, propertyPath, parentSchema, Schema);

            string path = parentPath + "/" + property;
            ExpandNode oExpand = ExpandNode.Create(oProperty, expand.Select, expand.Filter, expand.Orderby, Schema, ParameterCollection);
            oExpand.Path = path;

            oExpand.Children = new ExpandNode[expand.Children.Length];
            for (int i = 0; i < expand.Children.Length; i++)
            {
                oExpand.Children[i] = Compose(expand.Children[i], propertyPath[propertyPath.Length - 1], path);
            }

            return oExpand;
        }


    }
}
