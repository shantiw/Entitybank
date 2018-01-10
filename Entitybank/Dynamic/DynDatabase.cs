using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.DataObjects;
using XData.Data.Objects;

namespace XData.Data.Dynamic
{
    public class DynDatabase : Database<dynamic>
    {
        public event InsertingEventHandler Inserting;
        public event InsertedEventHandler Inserted;
        public event DeletingEventHandler Deleting;
        public event UpdatingEventHandler Updating;

        protected override void OnInserting(dynamic aggregNode, string entity, XElement schema, string path, dynamic aggreg)
        {
            InsertingEventArgs args = new InsertingEventArgs(aggregNode, entity, schema, path, aggreg);
            Inserting?.Invoke(this, args);
        }

        protected override void OnInserted(dynamic aggregNode, string entity, XElement schema, string path, dynamic aggreg, out IList<SQLStatment> after)
        {
            InsertedEventArgs args = new InsertedEventArgs(aggregNode, entity, schema, path, aggreg);
            Inserted?.Invoke(this, args);

            after = args.After;
        }

        protected override void OnDeleting(dynamic aggregNode, string entity, XElement schema, string path, dynamic aggreg,
            IReadOnlyDictionary<string, object> refetched, out IList<SQLStatment> before)
        {
            DeletingEventArgs args = new DeletingEventArgs(aggregNode, entity, schema, path, aggreg)
            {
                Refetched = refetched
            };

            Deleting?.Invoke(this, args);

            before = args.Before;
        }

        protected override void OnUpdating(dynamic aggregNode, string entity, XElement schema, string path, dynamic aggreg,
            Func<IReadOnlyDictionary<string, object>> refetch, out IList<SQLStatment> before, out IList<SQLStatment> after)
        {
            UpdatingEventArgs args = new UpdatingEventArgs(aggregNode, entity, schema, path, aggreg)
            {
                Refetch = refetch
            };
            Updating?.Invoke(this, args);

            before = args.Before;
            after = args.After;
        }

        public override IEnumerable<dynamic> SqlQuery(string entity, string sql, params object[] parameters)
        {
            throw new NotSupportedException();
        }

        protected DynDatabase(Database database) : base(database)
        {
        }

        public static DynDatabase Create(string name)
        {
            Database database = new DatabaseManufacturer().Create(name);
            return new DynDatabase(database);
        }


    }
}
