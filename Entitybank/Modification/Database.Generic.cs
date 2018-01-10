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
    public abstract partial class Database<T>
    {
        public string ParameterPrefix => UnderlyingDatabase.ParameterPrefix;

        internal protected Database UnderlyingDatabase { get; private set; }

        protected Database(Database database)
        {
            UnderlyingDatabase = database;
        }

        public DateTime GetNow()
        {
            return UnderlyingDatabase.GetNow();
        }

        public DateTime GetUtcNow()
        {
            return UnderlyingDatabase.GetUtcNow();
        }

        public DbConnection Connection { get => UnderlyingDatabase.Connection; }
        public DbTransaction Transaction { get => UnderlyingDatabase.Transaction; set => UnderlyingDatabase.Transaction = value; }

        public virtual int ExecuteSqlCommand(string sql, params object[] parameters)
        {
            return UnderlyingDatabase.ExecuteSqlCommand(sql, parameters);
        }

        public virtual IEnumerable<T> SqlQuery(string entity, string sql, params object[] parameters)
        {
            return UnderlyingDatabase.SqlQuery<T>(entity, sql, parameters);
        }

        public DbParameter CreateParameter(string parameter, object value)
        {
            return UnderlyingDatabase.CreateParameter(parameter, value);
        }


    }
}
