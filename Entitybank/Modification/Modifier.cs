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
        protected const string DEFAULT_DATE_FORMAT = TypeHelper.DEFAULT_DATE_FORMAT;

        //public Database<T> Database { get; private set; }
        protected Database<T> GenericDatabase { get; private set; }

        protected readonly IList<ExecuteAggregation<T>> ExecuteAggregations = new List<ExecuteAggregation<T>>();

        protected Modifier(Database<T> database)
        {
            GenericDatabase = database;
        }

        public virtual void Clear()
        {
            ExecuteAggregations.Clear();
        }

        // overload
        public void Create(T obj, string entity, XElement schema, out string keys)
        {
            Create(obj, entity, schema, out IEnumerable<Dictionary<string, object>> result);
            keys = KeysToJson(result);
        }

        // overload
        public void Create(T obj, string entity, XElement schema, out XElement keys)
        {
            Create(obj, entity, schema, out IEnumerable<Dictionary<string, object>> result);
            keys = KeysToXml(result, GetEntitySchema(schema, entity));
        }

        // overload
        public virtual void Create(T obj, string entity, XElement schema, out IEnumerable<Dictionary<string, object>> keys)
        {
            Create(obj, entity, schema);
            keys = GetCreateResult();
        }

        public virtual void Create(T obj, string entity, XElement schema)
        {
            if (IsCollection(obj))
            {
                foreach (T child in (obj as IEnumerable<T>))
                {
                    AppendCreate(child, entity, schema);
                }
            }
            else
            {
                AppendCreate(obj, entity, schema);
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

        public virtual void Delete(T obj, string entity, XElement schema)
        {
            if (IsCollection(obj))
            {
                foreach (T child in (obj as IEnumerable<T>))
                {
                    AppendDelete(child, entity, schema);
                }
            }
            else
            {
                AppendDelete(obj, entity, schema);
            }

            Validate();
            Persist();
        }

        public virtual void Update(T obj, string entity, XElement schema)
        {
            if (IsCollection(obj))
            {
                foreach (T child in (obj as IEnumerable<T>))
                {
                    AppendUpdate(child, entity, schema);
                }
            }
            else
            {
                AppendUpdate(obj, entity, schema);
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

        protected static XElement KeysToXml(IEnumerable<Dictionary<string, object>> keys, XElement entitySchema)
        {
            string entity = entitySchema.Attribute(SchemaVocab.Name).Value;

            List<XElement> elements = new List<XElement>();
            foreach (Dictionary<string, object> key in keys)
            {
                XElement element = new XElement(entity);
                foreach (KeyValuePair<string, object> pair in key)
                {
                    XElement property = new XElement(pair.Key);
                    Type type = pair.Value.GetType();
                    if (type == typeof(DateTime))
                    {
                        property.Value = ((DateTime)pair.Value).ToString(DEFAULT_DATE_FORMAT);
                    }
                    else if (type == typeof(bool))
                    {
                        property.Value = ((bool)pair.Value) ? "true" : "false";
                    }
                    else
                    {
                        property.Value = pair.Value.ToString();
                    }
                    element.Add(property);
                }
                elements.Add(element);
            }

            if (elements.Count == 1) return elements.First();

            string collection = entitySchema.Attribute(SchemaVocab.Collection).Value;
            XElement result = new XElement(collection);
            result.Add(elements);
            return result;
        }

        protected static string KeysToJson(IEnumerable<Dictionary<string, object>> keys)
        {
            List<string> jsons = new List<string>();
            foreach (Dictionary<string, object> key in keys)
            {
                List<string> keyValues = new List<string>();
                foreach (KeyValuePair<string, object> pair in key)
                {
                    Type type = pair.Value.GetType();
                    string value;
                    if (TypeHelper.IsNumeric(type))
                    {
                        value = pair.Value.ToString();
                    }
                    else if (type == typeof(bool))
                    {
                        value = ((bool)pair.Value) ? "true" : "false";
                    }
                    else if (type == typeof(DateTime))
                    {
                        value = "\"" + ((DateTime)pair.Value).ToString(DEFAULT_DATE_FORMAT) + "\"";
                    }
                    else
                    {
                        value = "\"" + pair.Value.ToString() + "\"";
                    }
                    keyValues.Add("\"" + pair.Key + "\":" + value);
                }
                jsons.Add("{" + string.Join(",", keyValues) + "}");
            }

            if (jsons.Count == 1) return jsons.First();

            return "[" + string.Join(",", jsons) + "]";
        }


    }
}
