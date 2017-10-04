using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.DataObjects;

namespace XData.Data.Dynamic
{
    public abstract class ExecuteEventArgs
    {
        public dynamic AggregNode { get; private set; }
        public string Entity { get; private set; }
        public dynamic Aggreg { get; private set; }
        public XElement Schema { get; private set; }
        public string Path { get; private set; }

        protected ExecuteEventArgs(dynamic aggregNode, string entity, XElement schema, string path, dynamic aggreg)
        {
            AggregNode = aggregNode;
            Entity = entity;
            Schema = schema;
            Path = path;
            Aggreg = aggreg;
        }
    }

    public class InsertingEventArgs : ExecuteEventArgs
    {
        public InsertingEventArgs(object aggregNode, string entity, XElement schema, string path, object aggreg)
            : base(aggregNode, entity, schema, path, aggreg)
        {
        }
    }

    public class InsertedEventArgs : ExecuteEventArgs
    {
        // sql, parameters
        public IList<SQLStatment> After { get; private set; } = new List<SQLStatment>();

        public InsertedEventArgs(object aggregNode, string entity, XElement schema, string path, object aggreg)
            : base(aggregNode, entity, schema, path, aggreg)
        {
        }
    }

    public class DeletingEventArgs : ExecuteEventArgs
    {
        public IReadOnlyDictionary<string, object> Refetched { get; internal set; }

        // sql, parameters
        public IList<SQLStatment> Before { get; private set; } = new List<SQLStatment>();

        public DeletingEventArgs(object aggregNode, string entity, XElement schema, string path, object aggreg)
            : base(aggregNode, entity, schema, path, aggreg)
        {
        }
    }

    public class UpdatingEventArgs : ExecuteEventArgs
    {
        internal Func<IReadOnlyDictionary<string, object>> Refetch;
        public IReadOnlyDictionary<string, object> Refetched { get => Refetch(); }

        // sql, parameters
        public IList<SQLStatment> Before { get; private set; } = new List<SQLStatment>();

        // sql, parameters
        public IList<SQLStatment> After { get; private set; } = new List<SQLStatment>();

        public UpdatingEventArgs(object aggregNode, string entity, XElement schema, string path, object aggreg)
            : base(aggregNode, entity, schema, path, aggreg)
        {
        }
    }

    public delegate void InsertingEventHandler(object sender, InsertingEventArgs args);
    public delegate void InsertedEventHandler(object sender, InsertedEventArgs args);
    public delegate void DeletingEventHandler(object sender, DeletingEventArgs args);
    public delegate void UpdatingEventHandler(object sender, UpdatingEventArgs args);

}
