using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.DataObjects;

namespace XData.Data.Xml
{
    public abstract class ExecuteEventArgs
    {
        public XElement Node { get; private set; }
        public XElement Element { get; private set; }
        public XElement Schema { get; private set; }
        public string Path { get; private set; }

        protected ExecuteEventArgs(XElement node, XElement schema, string path, XElement element)
        {
            Node = node;
            Schema = schema;
            Path = path;
            Element = element;
        }
    }

    public class InsertingEventArgs : ExecuteEventArgs
    {
        public InsertingEventArgs(XElement node, XElement schema, string path, XElement element)
            : base(node, schema, path, element)
        {
        }
    }

    public class InsertedEventArgs : ExecuteEventArgs
    {
        // sql, parameters
        public IList<SQLStatment> After { get; private set; } = new List<SQLStatment>();

        public InsertedEventArgs(XElement node, XElement schema, string path, XElement element)
            : base(node, schema, path, element)
        {
        }
    }

    public class DeletingEventArgs : ExecuteEventArgs
    {
        public IReadOnlyDictionary<string, object> Refetched { get; internal set; }

        // sql, parameters
        public IList<SQLStatment> Before { get; private set; } = new List<SQLStatment>();

        public DeletingEventArgs(XElement node, XElement schema, string path, XElement element)
            : base(node, schema, path, element)
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

        public UpdatingEventArgs(XElement node, XElement schema, string path, XElement element)
            : base(node, schema, path, element)
        {
        }
    }

    public delegate void InsertingEventHandler(object sender, InsertingEventArgs args);
    public delegate void InsertedEventHandler(object sender, InsertedEventArgs args);
    public delegate void DeletingEventHandler(object sender, DeletingEventArgs args);
    public delegate void UpdatingEventHandler(object sender, UpdatingEventArgs args);

}
