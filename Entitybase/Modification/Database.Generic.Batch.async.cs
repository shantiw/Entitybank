using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Modification;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    public abstract partial class Database<T>
    {
        public virtual async Task BatchInsertAsync(IEnumerable<T> objects, string entity, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(entity);

            Dictionary<string, object>[] dicts = ToDictionaries(objects, entitySchema);

            CheckAutoIncrement(dicts, entity, entitySchema);

            IEnumerable<BatchStatement> statments =
                ModificationGenerator.GenerateBatchInsertStatements(dicts, entitySchema);

            await PersistAsync(statments, false, null, null, null);
        }

        public virtual async Task BatchDeleteAsync(IEnumerable<T> objects, string entity, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(entity);
            XElement keySchema = schema.GetKeySchema(entity);
            XElement concurrencySchema = schema.GetConcurrencySchema(entity);

            Dictionary<string, object>[] dicts = ToDictionaries(objects, entitySchema);

            IEnumerable<BatchStatement> statments =
                ModificationGenerator.GenerateBatchDeleteStatements(dicts, entitySchema, keySchema, concurrencySchema);

            await PersistAsync(statments, concurrencySchema != null, dicts, entity, keySchema);
        }

        public virtual async Task BatchUpdateAsync(IEnumerable<T> objects, string entity, T value, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(entity);
            XElement keySchema = schema.GetKeySchema(entity);
            XElement concurrencySchema = schema.GetConcurrencySchema(entity);

            Dictionary<string, object>[] dicts = ToDictionaries(objects, entitySchema);
            Dictionary<string, object> valueDict = ToDictionary(value, entitySchema);

            IEnumerable<BatchStatement> statments =
               ModificationGenerator.GenerateBatchUpdateStatements(dicts, valueDict, entitySchema, keySchema, concurrencySchema);

            await PersistAsync(statments, concurrencySchema != null, dicts, entity, keySchema);
        }

        public virtual async Task BatchUpdateAsync(IEnumerable<T> objects, string entity, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(entity);
            XElement keySchema = schema.GetKeySchema(entity);
            XElement concurrencySchema = schema.GetConcurrencySchema(entity);

            Dictionary<string, object>[] dicts = ToDictionaries(objects, entitySchema);

            IEnumerable<BatchStatement> statments =
                ModificationGenerator.GenerateBatchUpdateStatements(dicts, entitySchema, keySchema, concurrencySchema);

            await PersistAsync(statments, concurrencySchema != null, dicts, entity, keySchema);
        }

        protected async Task PersistAsync(IEnumerable<BatchStatement> statments, bool concurrencyCheck, Dictionary<string, object>[] objects, string entity, XElement keySchema)
        {
            ConnectionState state = Connection.State;
            try
            {
                if (state == ConnectionState.Closed)
                {
                    await Connection.OpenAsync();
                    Transaction = Connection.BeginTransaction();
                }

                foreach (BatchStatement statment in statments)
                {
                    string sql = statment.Sql;
                    DbParameter[] parameters = CreateParameters(statment.Parameters);
                    int affected = await UnderlyingDatabase.ExecuteSqlCommandAsync(sql, parameters);

                    CheckConcurrency(affected, sql, parameters, statment, concurrencyCheck, objects, entity, keySchema);
                }

                if (state == ConnectionState.Closed)
                {
                    if (Transaction != null) Transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                if (state == ConnectionState.Closed)
                {
                    if (Transaction != null) Transaction.Rollback();
                }

                throw ex;
            }
            finally
            {
                if (state == ConnectionState.Closed)
                {
                    Connection.Close();
                    Transaction = null;
                }
            }
        }


    }
}
