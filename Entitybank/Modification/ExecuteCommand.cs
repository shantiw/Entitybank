using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Modification
{
    public abstract class ExecuteCommand<T>
    {
        public T AggregNode { get; private set; }
        public string Entity { get; private set; }
        public XElement Schema { get; private set; }
        public T Aggreg { get; private set; }

        public string Path { get; set; }

        public XElement EntitySchema { get; set; }
        public XElement UniqueKeySchema { get; set; } // Primary or Unique(ManyToMany) Key
        public Dictionary<string, object> PropertyValues { get; set; }
        public Dictionary<string, object> ParentPropertyValues { get; set; }
        public DirectRelationship ParentRelationship { get; set; } // OneToMany

        internal void SetAggregNode(T aggregNode)
        {
            AggregNode = aggregNode;
        }

        public ExecuteCommand(T aggregNode, string entity, XElement schema, T aggreg)
        {
            AggregNode = aggregNode;
            Entity = entity;
            Schema = schema;
            Aggreg = aggreg;
        }
    }

    public class InsertCommand<T> : ExecuteCommand<T>
    {
        public InsertCommand(T aggregNode, string entity, XElement schema, T aggreg)
              : base(aggregNode, entity, schema, aggreg)
        {
        }
    }

    // key values required
    public class DeleteCommand<T> : ExecuteCommand<T>
    {
        public XElement ConcurrencySchema { get; set; } = null; // include timestampSchema;

        public IEnumerable<DirectRelationship> ChildRelationships { get; set; } // relationship constraint

        public DeleteCommand(T aggregNode, string entity, XElement schema, T aggreg)
              : base(aggregNode, entity, schema, aggreg)
        {
        }
    }

    // key values required
    public class UpdateCommand<T> : ExecuteCommand<T>
    {
        public XElement ConcurrencySchema { get; set; } = null; // include timestampSchema;

        public Dictionary<string, object> FixedUpdatePropertyValues { get; set; } = new Dictionary<string, object>();

        public UpdateCommand(T aggregNode, string entity, XElement schema, T aggreg)
              : base(aggregNode, entity, schema, aggreg)
        {
        }
    }

    public class UpdateCommandNode<T> : UpdateCommand<T>
    {
        public T OrigNode { get; private set; }
        public T Original { get; private set; }

        public Dictionary<string, object> OrigPropertyValues { get; set; }

        public ICollection<UpdateCommandNodeChildren<T>> ChildrenCollection { get; set; } = new List<UpdateCommandNodeChildren<T>>();

        public UpdateCommandNode(T aggregNode, T origNode, string entity, XElement schema, T aggreg, T original)
              : base(aggregNode, entity, schema, aggreg)
        {
            OrigNode = origNode;
            Original = original;
        }
    }

    public class UpdateCommandNodeChildren<T>
    {
        public DirectRelationship ParentRelationship { get; private set; }
        public string Path { get; private set; }
        public ICollection<UpdateCommandNode<T>> UpdateCommandNodes = new List<UpdateCommandNode<T>>();

        public UpdateCommandNodeChildren(DirectRelationship oneToManyRelationship, string path)
        {
            ParentRelationship = oneToManyRelationship;
            Path = path;
        }
    }

}
