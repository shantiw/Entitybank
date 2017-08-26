using System.Collections.Generic;

namespace XData.Data.DataObjects
{
    public class ForeignKeyNode
    {
        public ForeignKey ForeignKey { get; private set; }
        public string Key { get; private set; }

        private List<ForeignKeyNode> _children = new List<ForeignKeyNode>();
        public ICollection<ForeignKeyNode> Children { get => _children; }

        public ForeignKeyNode(ForeignKey foreignKey)
        {
            ForeignKey = foreignKey;
            Key = ForeignKey.ToString();
        }


    }
}
