using XData.Data.Objects;
using XData.Data.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Services
{
    // name: ConnectionStringName
    public sealed class SchemaService
    {
        private SchemaProvider SchemaProvider;
        private PrimarySchemaProvider PrimarySchemaProvider = new PrimarySchemaProvider();
        private SchemaDeltaProvider SchemaDeltaProvider = new SchemaDeltaProvider();

        public XElement GetSchema(string name, IEnumerable<KeyValuePair<string, string>> deltaKey)
        {
            SchemaProvider = new SchemaProvider(name);
            return SchemaProvider.GetSchema(deltaKey);
        }

        public XElement GetSchema(string name, int level)
        {
            return PrimarySchemaProvider.GetSchema(name, (SchemaSource)level);
        }

        public void Update(string name, int level)
        {
            if (level == Enum.GetValues(typeof(SchemaSource)).Length)
            {
                SchemaDeltaProvider.Update(name);
            }
            else
            {
                PrimarySchemaProvider.Update(name, (SchemaSource)level);
            }
        }


    }
}
