using XData.Data.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Objects
{
    // name: ConnectionStringName
    public class SchemaProvider
    {
        public const char Value_Separator = ',';

        protected PrimarySchemaProvider PrimarySchemaProvider = new PrimarySchemaProvider();
        protected SchemaDeltaProvider SchemaDeltaProvider = new SchemaDeltaProvider();

        protected readonly string Name;

        public SchemaProvider(string name)
        {
            Name = name;
        }

        // ?key1=key1,key1&key2=,key2
        public XElement GetSchema(IEnumerable<KeyValuePair<string, string>> deltaKey)
        {
            Dictionary<int, Dictionary<string, string>> dict = new Dictionary<int, Dictionary<string, string>>();
            foreach (KeyValuePair<string, string> item in deltaKey)
            {
                string key = item.Key;
                string[] values = item.Value.Split(Value_Separator);
                for (int i = 0; i < values.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(values[i])) continue;

                    if (!dict.ContainsKey(i)) dict.Add(i, new Dictionary<string, string>());

                    dict[i].Add(key, values[i].Trim());
                }
            }

            //
            XElement schema = PrimarySchemaProvider.GetSchema(Name);
            foreach (KeyValuePair<int, Dictionary<string, string>> pair in dict)
            {
                XElement delta = SchemaDeltaProvider.Get(Name, pair.Value);
                schema.Modify(delta);
            }

            return schema;
        }


    }
}
