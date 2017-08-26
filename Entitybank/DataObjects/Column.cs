using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.DataObjects
{
    public sealed class Column
    {
        public string Table { get; private set; }
        public string Name { get; private set; }

        public string TableAlias { get; internal set; }

        public Column(string table, string name)
        {
            Table = table;
            Name = name;
        }
    }

}
