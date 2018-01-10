using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.DataObjects;
using XData.Data.Objects;

namespace XData.Data.Xml
{
    public class XmlDatabase : Database<XElement>
    {
        public event InsertingEventHandler Inserting;
        public event InsertedEventHandler Inserted;
        public event DeletingEventHandler Deleting;
        public event UpdatingEventHandler Updating;

        protected override void OnInserting(XElement aggregNode, string entity, XElement schema, string path, XElement aggreg)
        {
            InsertingEventArgs args = new InsertingEventArgs(aggregNode, schema, path, aggreg);
            Inserting?.Invoke(this, args);
        }

        protected override void OnInserted(XElement aggregNode, string entity, XElement schema, string path, XElement aggreg, out IList<SQLStatment> after)
        {
            InsertedEventArgs args = new InsertedEventArgs(aggregNode, schema, path, aggreg);
            Inserted?.Invoke(this, args);

            after = args.After;
        }

        protected override void OnDeleting(XElement aggregNode, string entity, XElement schema, string path, XElement aggreg,
            IReadOnlyDictionary<string, object> refetched, out IList<SQLStatment> before)
        {
            DeletingEventArgs args = new DeletingEventArgs(aggregNode, schema, path, aggreg)
            {
                Refetched = refetched
            };
            Deleting?.Invoke(this, args);

            before = args.Before;
        }

        protected override void OnUpdating(XElement aggregNode, string entity, XElement schema, string path, XElement aggreg,
            Func<IReadOnlyDictionary<string, object>> refetch, out IList<SQLStatment> before, out IList<SQLStatment> after)
        {
            UpdatingEventArgs args = new UpdatingEventArgs(aggregNode, schema, path, aggreg)
            {
                Refetch = refetch
            };
            Updating?.Invoke(this, args);

            before = args.Before;
            after = args.After;
        }

        protected XmlDatabase(Database database) : base(database)
        {
        }

        public static XmlDatabase Create(string name)
        {
            Database database = new DatabaseManufacturer().Create(name);
            return new XmlDatabase(database);
        }


    }
}
