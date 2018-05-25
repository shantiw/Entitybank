using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    public class Select
    {
        public string[] Properties { get; internal set; }

        protected readonly string Value;
        protected readonly string Entity;
        protected readonly XElement Schema;

        public Select(string value, string entity, XElement schema)
        {
            Value = value;
            Entity = entity;
            Schema = schema;

            if (!string.IsNullOrWhiteSpace(value))
            {
                if (value.Trim() == "*")
                {
                    XElement entitySchema = Schema.GetEntitySchema(Entity);
                    Properties = entitySchema.Elements(SchemaVocab.Property)
                        .Where(x => x.Attribute(SchemaVocab.Column) != null)
                        .Select(x => x.Attribute(SchemaVocab.Name).Value).ToArray();
                }
                else
                {
                    Properties = value.Split(',');
                    for (int i = 0; i < Properties.Length; i++)
                    {
                        Properties[i] = Properties[i].Trim();
                    }
                }
            }
        }


    }
}
