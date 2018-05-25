using System.Collections.Generic;
using System.Data;

namespace XData.Data.Objects
{
    public class DataRowNode
    {
        public DataRow Row { get; private set; }
        public string Entity { get; private set; }

        public Dictionary<string, DataRowNode> ChildDict { get; private set; } = new Dictionary<string, DataRowNode>();
        public Dictionary<string, ICollection<DataRowNode>> ChildrenDict { get; private set; } = new Dictionary<string, ICollection<DataRowNode>>();

        public DataRowNode(DataRow row, string entity = null)
        {
            Row = row;
            Entity = entity;
        }
    }
}
