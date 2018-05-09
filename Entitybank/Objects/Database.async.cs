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
        public virtual async Task<int> ExecuteSqlCommandAsync(string sql, params object[] parameters)
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
                await cmd.Connection.OpenAsync();
            }
            try
            {
                // System.InvalidOperationException: SqlCommand/OleDbCommand.Prepare method requires all parameters to have an explicitly set type.
                //cmd.Prepare();
                int affected = await cmd.ExecuteNonQueryAsync();
                return affected;
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

        internal protected async Task<object> ExecuteScalarAsync(string sql, params object[] parameters)
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
                await cmd.Connection.OpenAsync();
            }
            try
            {
                object result = await cmd.ExecuteScalarAsync();
                return result;
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


    }
}
