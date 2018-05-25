using System.Collections.Generic;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    // name: ConnectionStringName
    public abstract class SchemaDeltaFinder
    {
        protected readonly string Name;

        public SchemaDeltaFinder(string name)
        {
            Name = name;
        }

        public abstract XElement Find(IEnumerable<KeyValuePair<string, string>> keyValues);

    }
}
