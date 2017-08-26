using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.DataObjects;
using XData.Data.Objects;

namespace XData.Data.OData
{
    public abstract partial class TempTableResultGetter : QueryExpandResultGetter
    {
        protected abstract class TempTableNode
        {
            public string Name { get; private set; }
            public string[] Select { get; private set; }
            public string Entity { get; private set; }

            // parent:Properties, this:RelatedProperties // first DirectRelationship
            public Dictionary<string, string> RelatedKey { get; set; }

            public List<TempTableNode> Children { get; set; } = new List<TempTableNode>();

            public string Path { get; set; }

            public string ParentTempTableName { get; set; }
            public string TempTableName { get; set; }
            public IEnumerable<string> DistinctKey { get; set; }

            public List<SQLStatment> BeforeExecuteStatments { get; set; } = new List<SQLStatment>();
            public SQLStatment FetchTableStatment { get; set; }
            public List<SQLStatment> AfterExecuteStatments { get; set; } = new List<SQLStatment>();

            public DataTable Table { get; set; }

            protected TempTableNode(string name, string[] select, string entity)
            {
                Name = name;
                Select = select;
                Entity = entity;
            }

            //
            public ResultNode ToResultNode()
            {
                return ToResultNode(this);
            }

            protected static ResultNode ToResultNode(TempTableNode node)
            {
                ResultNode resultNode;
                if (node is EntityTempNode)
                {
                    resultNode = new EntityResultNode(node.Name, node.Select, node.Table, node.RelatedKey, node.Entity);
                }
                else if (node is CollectionTempNode)
                {
                    CollectionTempNode collectionTempNode = node as CollectionTempNode;
                    resultNode = new CollectionResultNode(node.Name, node.Select, collectionTempNode.OrderBy, node.Table, node.RelatedKey, collectionTempNode.Entity);
                }
                else
                {
                    throw new NotSupportedException(node.GetType().ToString()); // never 
                }
                resultNode.Path = node.Path;

                List<ResultNode> resultChildren = new List<ResultNode>();
                foreach (TempTableNode child in node.Children)
                {
                    resultChildren.Add(ToResultNode(child));
                }
                resultNode.Children = resultChildren.ToArray();

                return resultNode;
            }

        }

        protected class CollectionTempNode : TempTableNode
        {
            public Order[] OrderBy { get; private set; }

            public CollectionTempNode(string name, string[] select, Order[] orderby, string entity) : base(name, select, entity)
            {
                OrderBy = orderby;
            }
        }

        protected class EntityTempNode : TempTableNode
        {
            public EntityTempNode(string name, string[] select, string entity) : base(name, select, entity)
            {
            }
        }

    }
}
