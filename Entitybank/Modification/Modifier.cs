using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Helpers;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.Modification
{
    public abstract partial class Modifier<T>
    {
        //public Database<T> Database { get; private set; }
        protected Database<T> GenericDatabase { get; private set; }
        protected XElement Schema { get; private set; } // default

        protected readonly IList<ExecuteAggregation<T>> ExecuteAggregations = new List<ExecuteAggregation<T>>();

        protected Modifier(Database<T> database, XElement schema)
        {
            GenericDatabase = database;
            Schema = schema;
        }

        public virtual void Clear()
        {
            ExecuteAggregations.Clear();
        }

        // overload
        public void Create(T obj, string entity, out IEnumerable<Dictionary<string, object>> keys)
        {
            Create(obj, entity);
            keys = GetCreateResult();
        }

        // overload
        public void Create(T obj, string entity, XElement schema, out IEnumerable<Dictionary<string, object>> keys)
        {
            Create(obj, entity, schema);
            keys = GetCreateResult();
        }

        public virtual void Create(T obj, string entity, XElement schema = null)
        {
            XElement xSchema = schema ?? Schema;

            if (IsCollection(obj))
            {
                foreach (T child in (obj as IEnumerable<T>))
                {
                    AppendCreate(child, entity, xSchema);
                }
            }
            else
            {
                AppendCreate(obj, entity, xSchema);
            }

            Validate();
            Persist();
        }

        // GetKeyPropertyValues
        protected IEnumerable<Dictionary<string, object>> GetCreateResult()
        {
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            foreach (ExecuteAggregation<T> executeAggregation in ExecuteAggregations)
            {
                ExecuteCommand<T> executeCommand = executeAggregation.ExecuteCommands.First();

                Dictionary<string, object> dict = new Dictionary<string, object>();
                IEnumerable<string> keyPropertyNames = executeCommand.UniqueKeySchema.Elements(SchemaVocab.Property).Select(p => p.Attribute(SchemaVocab.Name).Value);
                foreach (string propertyName in keyPropertyNames)
                {
                    dict.Add(propertyName, dict[propertyName]);
                }

                list.Add(dict);
            }
            return list;
        }

        public virtual void Delete(T obj, string entity, XElement schema = null)
        {
            XElement xSchema = schema ?? Schema;

            if (IsCollection(obj))
            {
                foreach (T child in (obj as IEnumerable<T>))
                {
                    AppendDelete(child, entity, xSchema);
                }
            }
            else
            {
                AppendDelete(obj, entity, xSchema);
            }

            Validate();
            Persist();
        }

        public virtual void Update(T obj, string entity, XElement schema = null)
        {
            XElement xSchema = schema ?? Schema;

            if (IsCollection(obj))
            {
                foreach (T child in (obj as IEnumerable<T>))
                {
                    AppendUpdate(child, entity, xSchema);
                }
            }
            else
            {
                AppendUpdate(obj, entity, xSchema);
            }

            Validate();
            Persist();
        }

        public void Persist()
        {
            ConnectionState state = GenericDatabase.Connection.State;
            try
            {
                if (state == ConnectionState.Closed)
                {
                    GenericDatabase.Connection.Open();
                    GenericDatabase.Transaction = GenericDatabase.Connection.BeginTransaction();
                }

                foreach (ExecuteAggregation<T> executeAggregation in ExecuteAggregations)
                {
                    foreach (ExecuteCommand<T> executeCommand in executeAggregation.ExecuteCommands)
                    {
                        if (executeCommand is UpdateCommandNode<T>)
                        {
                            int i = GenericDatabase.Execute(executeCommand as UpdateCommandNode<T>, this);
                        }
                        else
                        {
                            if (executeCommand is InsertCommand<T>)
                            {
                                int i = GenericDatabase.Execute(executeCommand as InsertCommand<T>, this);
                            }
                            else if (executeCommand is DeleteCommand<T>)
                            {
                                int i = GenericDatabase.Execute(executeCommand as DeleteCommand<T>, this);
                            }
                            else if (executeCommand is UpdateCommand<T>) // DeleteAggregation SetNull
                            {
                                int i = GenericDatabase.Execute(executeCommand as UpdateCommand<T>, this);
                            }
                        }
                    }
                }

                if (state == ConnectionState.Closed)
                {
                    if (GenericDatabase.Transaction != null) GenericDatabase.Transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                if (state == ConnectionState.Closed)
                {
                    if (GenericDatabase.Transaction != null) GenericDatabase.Transaction.Rollback();
                }

                throw ex;
            }
            finally
            {
                if (state == ConnectionState.Closed)
                {
                    GenericDatabase.Connection.Close();
                    GenericDatabase.Transaction = null;
                }

                ExecuteAggregations.Clear();
            }
        }

        // overload
        public void AppendCreate(T aggreg, string entity)
        {
            AppendCreate(aggreg, entity, Schema);
        }

        // overload
        public void AppendDelete(T aggreg, string entity)
        {
            AppendDelete(aggreg, entity, Schema);
        }

        // overload
        public void AppendUpdate(T aggreg, string entity)
        {
            AppendUpdate(aggreg, entity, Schema);
        }

        public abstract void AppendCreate(T aggreg, string entity, XElement schema);
        public abstract void AppendDelete(T aggreg, string entity, XElement schema);
        public abstract void AppendUpdate(T aggreg, string entity, XElement schema);

        internal protected abstract T CreateObject(Dictionary<string, object> propertyValues, string entity);
        internal protected abstract Dictionary<string, object> GetPropertyValues(T obj, string entity, XElement schema);
        internal protected abstract void SetObjectValue(T obj, string property, object value);

        protected abstract bool IsCollection(T obj);

        //protected static Database<T> CreateDatabase(string name)
        //{
        //    return new Database<T>(new DatabaseManufacturer().Create(name));
        //}
        protected static Database CreateDatabase(string name)
        {
            return new DatabaseManufacturer().Create(name);
        }

        protected static XElement GetEntitySchema(XElement schema, string entity)
        {
            return schema.GetEntitySchema(entity);
        }

        protected static XElement GetEntitySchemaByCollection(XElement schema, string collection)
        {
            return schema.GetEntitySchemaByCollection(collection);
        }


    }
}
