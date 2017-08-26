using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Helpers;
using XData.Data.Modification;

namespace XData.Data.Objects
{
    public abstract partial class Database
    {
        public abstract DateTime GetUtcNow();
        public abstract DateTime GetNow();

        protected abstract DbConnection CreateConnection(string connectionString);
        protected abstract DbDataAdapter CreateDataAdapter();
        protected abstract DbParameter CreateParameter(string parameter, object value);

        public readonly DbConnection Connection;
        public DbTransaction Transaction = null;

        public Database(string connectionString)
        {
            Connection = CreateConnection(connectionString);
        }

        public virtual int ExecuteSqlCommand(string sql, params object[] parameters)
        {
            DbCommand cmd = Connection.CreateCommand();
            if (Transaction != null)
            {
                cmd.Transaction = Transaction;
            }
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters);
            ConnectionState state = cmd.Connection.State;
            if (state == ConnectionState.Closed)
            {
                cmd.Connection.Open();
            }
            try
            {
                // System.InvalidOperationException: SqlCommand/OleDbCommand.Prepare method requires all parameters to have an explicitly set type.
                //cmd.Prepare();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new SQLStatmentException(ex, sql, parameters);
            }
            finally
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }
        }

        // DataRow(entity = null), string(json)(entity = null), XElement
        public virtual IEnumerable<T> SqlQuery<T>(string entity, string sql, params Object[] parameters)
        {
            DataTable table = ExecuteDataTable(sql, parameters);

            T[] array = new T[table.Rows.Count];
            if (typeof(T) == typeof(DataRow))
            {
                table.Rows.CopyTo(array, 0);
                return array;
            }
            else if (typeof(T) == typeof(string))
            {
                new ToXmlConverter().Convert(table, entity).ToArray().CopyTo(array, 0);
            }
            else if (typeof(T) == typeof(XElement))
            {
                new ToJsonConverter().Convert(table, entity).ToArray().CopyTo(array, 0);
            }
            else
            {
                throw new NotSupportedException(typeof(T).ToString());
            }

            return array;
        }

        internal protected DataTable ExecuteDataTable(string sql, params object[] parameters)
        {
            DataTable dataTable = new DataTable();
            DbCommand cmd = Connection.CreateCommand();
            if (Transaction != null)
            {
                cmd.Transaction = Transaction;
            }
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters);
            DbDataAdapter da = CreateDataAdapter();
            da.SelectCommand = cmd;
            ConnectionState state = cmd.Connection.State;
            if (state == ConnectionState.Closed)
            {
                cmd.Connection.Open();
            }
            try
            {
                int i = da.Fill(dataTable);
                return dataTable;
            }
            catch (Exception ex)
            {
                throw new SQLStatmentException(ex, sql, parameters);
            }
            finally
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }
        }

        internal protected object ExecuteScalar(string sql, params object[] parameters)
        {
            DbCommand cmd = Connection.CreateCommand();
            if (Transaction != null)
            {
                cmd.Transaction = Transaction;
            }
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters);
            ConnectionState state = cmd.Connection.State;
            if (state == ConnectionState.Closed)
            {
                cmd.Connection.Open();
            }
            try
            {
                return cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new SQLStatmentException(ex, sql, parameters);
            }
            finally
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }
        }

        internal protected abstract ModificationGenerator CreateModificationGenerator();

        internal protected DbParameter[] CreateParameters(IReadOnlyDictionary<string, object> dbParameterValues)
        {
            List<DbParameter> list = new List<DbParameter>();
            foreach (KeyValuePair<string, object> pair in dbParameterValues)
            {
                object value = (pair.Value == null) ? DBNull.Value : pair.Value;
                list.Add(CreateParameter(pair.Key, value));
            }
            return list.ToArray();
        }

        internal protected abstract int ExecuteInsertCommand(string sql, object[] parameters, out object autoIncrementValue);

        internal protected abstract object FetchSequence(string sequenceName);

    }
}
