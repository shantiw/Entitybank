using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Objects
{
    public partial class Database<T>
    {
        protected readonly Database Dbase;

        public Database(Database database)
        {
            Dbase = database;
        }

        public DateTime GetNow()
        {
            return Dbase.GetNow();
        }

        public DateTime GetUtcNow()
        {
            return Dbase.GetUtcNow();
        }

        public DbConnection Connection { get => Dbase.Connection; }
        public DbTransaction Transaction { get => Dbase.Transaction; set => Dbase.Transaction = value; }

        public int ExecuteSqlCommand(string sql, params object[] parameters)
        {
            return Dbase.ExecuteSqlCommand(sql, parameters);
        }

        public IEnumerable<T> SqlQuery(string entity, string sql, params Object[] parameters)
        {
            return Dbase.SqlQuery<T>(entity, sql, parameters);
        }


    }
}
