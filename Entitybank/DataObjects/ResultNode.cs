using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Objects;

namespace XData.Data.DataObjects
{
    public abstract class ResultNode
    {
        public string Name { get; private set; }
        public string[] Select { get; private set; }
        public string Entity { get; private set; }
        public DataTable Table { get; private set; }

        // parent's columns, this's columns
        public IReadOnlyDictionary<string, string> RelatedKey { get; private set; }

        public ResultNode[] Children { get; internal set; } = new ResultNode[0];

        public string Path { get; set; }

        protected ResultNode(string name, string[] select, DataTable table, IReadOnlyDictionary<string, string> relatedKey, string entity)
        {
            Name = name;
            Select = select;
            Table = table;
            RelatedKey = relatedKey;
            Entity = entity;
        }
    }

    public class CollectionResultNode : ResultNode
    {
        public Order[] OrderBy { get; private set; }

        public CollectionResultNode(string name, string[] select, Order[] orderby, DataTable table, IReadOnlyDictionary<string, string> relatedKey, string entity)
            : base(name, select, table, relatedKey, entity)
        {
            OrderBy = orderby;

        }
    }

    public class EntityResultNode : ResultNode
    {
        public DataRow Row { get => (Table.Rows.Count == 0) ? null : Table.Rows[0]; }

        public EntityResultNode(string name, string[] select, DataTable table, IReadOnlyDictionary<string, string> relatedKey, string entity)
            : base(name, select, table, relatedKey, entity)
        {
        }
    }

}
