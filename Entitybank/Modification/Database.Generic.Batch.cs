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

            SetSequenceValues(dicts, entitySchema);

            IEnumerable<BatchStatement> statments =
                ModificationGenerator.GenerateBatchInsertStatements(dicts, entitySchema);
            Persist(statments, false, null, null, null);
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

        protected void SetSequenceValues(IEnumerable<Dictionary<string, object>> objects, XElement entitySchema)
        {
            Dictionary<string, string> propertySequenceNames = GetPropertySequenceNames(entitySchema);
            if (propertySequenceNames.Count == 0) return;

            Dictionary<string, object> first = objects.First();

            foreach (string property in propertySequenceNames.Keys.ToArray())
            {
                if (first.ContainsKey(property))
                {
                    if (!string.IsNullOrWhiteSpace(first[property]?.ToString()))
                    {
                        propertySequenceNames.Remove(property);
                    }
                }
            }

            int size = objects.Count();
            Dictionary<string, object[]> propertySequences = new Dictionary<string, object[]>();
            foreach (KeyValuePair<string, string> propertySequenceName in propertySequenceNames)
            {
                object[] values = UnderlyingDatabase.FetchSequences(propertySequenceName.Value, size);
                propertySequences.Add(propertySequenceName.Key, values);
            }

            int index = 0;
            foreach (Dictionary<string, object> obj in objects)
            {
                foreach (KeyValuePair<string, object[]> propertySequence in propertySequences)
                {
                    string key = propertySequence.Key;
                    object value = propertySequence.Value[index];
                    if (obj.ContainsKey(propertySequence.Key))
                    {
                        obj[key] = value;
                    }
                    else
                    {
                        obj.Add(key, value);
                    }
                }
                index++;
            }
        }

        private static Dictionary<string, string> GetPropertySequenceNames(XElement entitySchema)
        {
            Dictionary<string, string> propertySequenceNames = new Dictionary<string, string>();
            foreach (XElement propertySchema in entitySchema.Elements(SchemaVocab.Property).Where(p => p.Attribute(SchemaVocab.Sequence) != null))
            {
                if (propertySchema.Attribute(SchemaVocab.AutoIncrement) != null && propertySchema.Attribute(SchemaVocab.AutoIncrement).Value == "true") continue;

                string propertyName = propertySchema.Attribute(SchemaVocab.Name).Value;
                string sequenceName = propertySchema.Attribute(SchemaVocab.Sequence).Value;
                propertySequenceNames.Add(propertyName, sequenceName);
            }

            return propertySequenceNames;
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
                    int affected = ExecuteSqlCommand(sql, parameters);
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
