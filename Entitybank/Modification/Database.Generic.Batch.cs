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
        public virtual void BatchInsert(IEnumerable<T> objects, string entity, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(entity);

            Dictionary<string, object>[] dicts = ToDictionaries(objects, entitySchema);

            CheckAutoIncrement(dicts, entity, entitySchema);

            IEnumerable<BatchStatement> statments =
                ModificationGenerator.GenerateBatchInsertStatements(dicts, entitySchema);
            Persist(statments, false, null, null, null);
        }

        protected void CheckAutoIncrement(Dictionary<string, object>[] dicts, string entity, XElement entitySchema)
        {
            foreach (string property in entitySchema.Elements(SchemaVocab.Property)
                .Where(x => x.Attribute(SchemaVocab.AutoIncrement) != null && x.Attribute(SchemaVocab.AutoIncrement).Value == "true")
                .Select(x => x.Attribute(SchemaVocab.Name).Value))
            {
                foreach (Dictionary<string, object> dict in dicts.Where(d => d.ContainsKey(property)))
                {
                    if (dict[property] == null || dict[property] == DBNull.Value)
                    {
                        dict.Remove(property);
                        continue;
                    }

                    string errorMessage = string.Format(ErrorMessages.Constraint_InsertExplicitAutoIncrement, property, entity);
                    throw new ConstraintException(errorMessage);
                }
            }
        }

        public virtual void BatchDelete(IEnumerable<T> objects, string entity, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(entity);
            XElement keySchema = schema.GetKeySchema(entity);
            XElement concurrencySchema = schema.GetConcurrencySchema(entity);

            Dictionary<string, object>[] dicts = ToDictionaries(objects, entitySchema);

            IEnumerable<BatchStatement> statments =
                ModificationGenerator.GenerateBatchDeleteStatements(dicts, entitySchema, keySchema, concurrencySchema);

            Persist(statments, concurrencySchema != null, dicts, entity, keySchema);
        }

        public virtual void BatchUpdate(IEnumerable<T> objects, string entity, T value, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(entity);
            XElement keySchema = schema.GetKeySchema(entity);
            XElement concurrencySchema = schema.GetConcurrencySchema(entity);

            Dictionary<string, object>[] dicts = ToDictionaries(objects, entitySchema);
            Dictionary<string, object> valueDict = ToDictionary(value, entitySchema);

            IEnumerable<BatchStatement> statments =
               ModificationGenerator.GenerateBatchUpdateStatements(dicts, valueDict, entitySchema, keySchema, concurrencySchema);

            Persist(statments, concurrencySchema != null, dicts, entity, keySchema);
        }

        public virtual void BatchUpdate(IEnumerable<T> objects, string entity, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(entity);
            XElement keySchema = schema.GetKeySchema(entity);
            XElement concurrencySchema = schema.GetConcurrencySchema(entity);

            Dictionary<string, object>[] dicts = ToDictionaries(objects, entitySchema);

            IEnumerable<BatchStatement> statments =
                ModificationGenerator.GenerateBatchUpdateStatements(dicts, entitySchema, keySchema, concurrencySchema);

            Persist(statments, concurrencySchema != null, dicts, entity, keySchema);
        }

        protected abstract Dictionary<string, object> ToDictionary(T obj, XElement entitySchema);

        protected Dictionary<string, object>[] ToDictionaries(IEnumerable<T> objects, XElement entitySchema)
        {
            List<Dictionary<string, object>> dicts = new List<Dictionary<string, object>>();
            foreach (T obj in objects)
            {
                dicts.Add(ToDictionary(obj, entitySchema));
            }
            return dicts.ToArray();
        }

        protected void Persist(IEnumerable<BatchStatement> statments, bool concurrencyCheck, Dictionary<string, object>[] objects, string entity, XElement keySchema)
        {
            ConnectionState state = Connection.State;
            try
            {
                if (state == ConnectionState.Closed)
                {
                    Connection.Open();
                    Transaction = Connection.BeginTransaction();
                }

                foreach (BatchStatement statment in statments)
                {
                    string sql = statment.Sql;
                    DbParameter[] parameters = CreateParameters(statment.Parameters);
                    int affected = UnderlyingDatabase.ExecuteSqlCommand(sql, parameters);

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

        protected void CheckConcurrency(int affected, string sql, DbParameter[] parameters, BatchStatement statment, bool concurrencyCheck, Dictionary<string, object>[] objects, string entity, XElement keySchema)
        {
            if (concurrencyCheck)
            {
                if (statment.EndIndex - statment.StartIndex + 1 != affected)
                {
                    string valueMessage = GetKeyValueMessage(objects, statment.StartIndex, statment.EndIndex, keySchema);
                    throw new OptimisticConcurrencyException(
                        string.Format(ErrorMessages.OptimisticConcurrencyException, entity, valueMessage), sql, parameters);
                }
            }
        }

        private string GetKeyValueMessage(Dictionary<string, object>[] objects, int startIndex, int endIndex, XElement keySchema)
        {
            List<string> list = new List<string>();
            foreach (Dictionary<string, object> propertyValues in objects)
            {
                List<string> values = new List<string>();
                foreach (XElement propertySchema in keySchema.Elements(SchemaVocab.Property))
                {
                    string propertyName = propertySchema.Attribute(SchemaVocab.Name).Value;
                    values.Add("'" + propertyValues[propertyName].ToString() + "'");
                }
                if (values.Count == 1)
                {
                    list.Add(values.First());
                }
                else
                {
                    list.Add("(" + string.Join(",", values) + ")");
                }
            }
            return "[" + string.Join(",", list) + "]";
        }


    }
}
