using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.DataObjects;
using XData.Data.Modification;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    // Database.Modification.cs
    //public partial class Database<T>
    public abstract partial class Database<T>
    {
        //public event InsertingEventHandler<T> Inserting;
        //public event InsertedEventHandler<T> Inserted;
        //public event DeletingEventHandler<T> Deleting;
        //public event UpdatingEventHandler<T> Updating;

        //protected void OnInserting(InsertingEventArgs<T> args)
        //{
        //    Inserting?.Invoke(this, args);
        //}
        protected abstract void OnInserting(T aggregNode, string entity, XElement schema, string path, T aggreg);

        //protected void OnInserted(InsertedEventArgs<T> args)
        //{
        //    Inserted?.Invoke(this, args);
        //}
        protected abstract void OnInserted(T aggregNode, string entity, XElement schema, string path, T aggreg, out IList<SQLStatment> after);

        //protected void OnDeleting(DeletingEventArgs<T> args)
        //{
        //    Deleting?.Invoke(this, args);
        //}
        protected abstract void OnDeleting(T aggregNode, string entity, XElement schema, string path, T aggreg,
            IReadOnlyDictionary<string, object> refetched, out IList<SQLStatment> before);

        //protected void OnUpdating(UpdatingEventArgs<T> args)
        //{
        //    Updating?.Invoke(this, args);
        //}
        protected abstract void OnUpdating(T aggregNode, string entity, XElement schema, string path, T aggreg,
            Func<IReadOnlyDictionary<string, object>> refetch, out IList<SQLStatment> before, out IList<SQLStatment> after);

        private ModificationGenerator _modificationGenerator = null;
        protected ModificationGenerator ModificationGenerator
        {
            get
            {
                if (_modificationGenerator == null)
                {
                    _modificationGenerator = Dbase.CreateModificationGenerator();
                }
                return _modificationGenerator;
            }
        }

        internal protected int Execute(InsertCommand<T> executeCommand, Modifier<T> modifier)
        {
            // establishing a relationship with parent
            if (executeCommand.ParentRelationship != null)
            {
                for (int i = 0; i < executeCommand.ParentRelationship.Properties.Length; i++)
                {
                    string parentProperty = executeCommand.ParentRelationship.Properties[i];
                    object value = executeCommand.ParentPropertyValues[parentProperty];
                    string property = executeCommand.ParentRelationship.RelatedProperties[i];
                    if (executeCommand.ParentPropertyValues.ContainsKey(property))
                    {
                        executeCommand.PropertyValues[property] = value;
                    }
                    else
                    {
                        executeCommand.PropertyValues.Add(property, value);
                    }
                }
            }

            // Sequence
            foreach (XElement propertySchema in executeCommand.EntitySchema.Elements(SchemaVocab.Property).Where(p => p.Attribute(SchemaVocab.Sequence) != null))
            {
                if (propertySchema.Attribute(SchemaVocab.AutoIncrement) != null && propertySchema.Attribute(SchemaVocab.AutoIncrement).Value == "true") continue;

                string propertyName = propertySchema.Attribute(SchemaVocab.Name).Value;

                // provided by user
                if (executeCommand.PropertyValues.ContainsKey(propertyName))
                {
                    if (executeCommand.PropertyValues[propertyName] != null) continue;
                }

                string sequenceName = propertySchema.Attribute(SchemaVocab.Sequence).Value;
                object sequence = Dbase.FetchSequence(sequenceName);
                if (executeCommand.PropertyValues.ContainsKey(propertyName))
                {
                    executeCommand.PropertyValues[propertyName] = sequence;
                }
                else
                {
                    executeCommand.PropertyValues.Add(propertyName, sequence);
                }
                modifier.SetObjectValue(executeCommand.AggregNode, propertyName, sequence);
            }

            // patch up SetDefaultValues
            foreach (KeyValuePair<string, object> pair in executeCommand.PropertyValues)
            {
                modifier.SetObjectValue(executeCommand.AggregNode, pair.Key, pair.Value);
            }

            // raise inserting event
            OnInserting(executeCommand.AggregNode, executeCommand.Entity, executeCommand.Schema, executeCommand.Path, executeCommand.Aggreg);

            // synchronize propertyValues with modified aggregNode OnInserting
            SynchronizePropertyValues(executeCommand, modifier);

            // validate
            modifier.Validate(executeCommand);

            // GenerateInsertStatement
            string sql = ModificationGenerator.GenerateInsertStatement(executeCommand.PropertyValues, executeCommand.EntitySchema,
                out IReadOnlyDictionary<string, object> dbParameterValues);
            DbParameter[] dbParameters = Dbase.CreateParameters(dbParameterValues);

            // AutoIncrement
            int affected;
            XElement autoPropertySchema = executeCommand.EntitySchema.Elements(SchemaVocab.Property).FirstOrDefault(p =>
                p.Attribute(SchemaVocab.AutoIncrement) != null && p.Attribute(SchemaVocab.AutoIncrement).Value == "true");
            if (autoPropertySchema == null)
            {
                affected = Dbase.ExecuteSqlCommand(sql, dbParameters);
            }
            else
            {
                affected = Dbase.ExecuteInsertCommand(sql, dbParameters, out object autoIncrementValue);

                string propertyName = autoPropertySchema.Attribute(SchemaVocab.Name).Value;
                if (executeCommand.PropertyValues.ContainsKey(propertyName))
                {
                    executeCommand.PropertyValues[propertyName] = autoIncrementValue;
                }
                else
                {
                    executeCommand.PropertyValues.Add(propertyName, autoIncrementValue);
                }
                modifier.SetObjectValue(executeCommand.AggregNode, propertyName, autoIncrementValue);
            }

            // raise inserted event
            OnInserted(executeCommand.AggregNode, executeCommand.Entity, executeCommand.Schema, executeCommand.Path, executeCommand.Aggreg, out IList<SQLStatment> after);

            foreach (SQLStatment statment in after)
            {
                int i = Dbase.ExecuteSqlCommand(statment.Sql, statment.Parameters);
            }

            return affected;
        }

        internal protected int Execute(DeleteCommand<T> executeCommand, Modifier<T> modifier)
        {
            // foreign key constraint check
            IReadOnlyDictionary<string, object> refetched = FetchSingleFromDb(executeCommand);

            foreach (DirectRelationship relationship in executeCommand.ChildRelationships)
            {
                XElement relatedEntitySchema = executeCommand.Schema.GetEntitySchema(relationship.RelatedEntity);

                XElement relatedKeySchema = new XElement(relatedEntitySchema.Name);
                Dictionary<string, object> relatedPropertyValues = new Dictionary<string, object>();
                for (int i = 0; i < relationship.Properties.Length; i++)
                {
                    string relatedProperty = relationship.RelatedProperties[i];
                    XElement relatedPropertySchema = relatedEntitySchema.Elements(SchemaVocab.Property).First(p => p.Attribute(SchemaVocab.Name).Value == relatedProperty);
                    relatedKeySchema.Add(relatedPropertySchema);

                    string property = relationship.Properties[i];
                    relatedPropertyValues.Add(relatedProperty, refetched[property]);
                }

                bool isExists = IsExistsInDb(relatedPropertyValues, relatedEntitySchema, relatedKeySchema);
                if (isExists)
                {
                    string relatedEntityName = relatedEntitySchema.Attribute(SchemaVocab.Name).Value;
                    IEnumerable<string> relatedPropertyNames = relatedKeySchema.Elements(SchemaVocab.Property).Select(p => "'" + p.Attribute(SchemaVocab.Name).Value + "'");
                    throw new ConstraintException(string.Format(ErrorMessages.RelationshipKeyConflicted,
                        executeCommand.Entity, GetKeyValueMessage(executeCommand),
                        relatedEntityName, string.Join(",", relatedPropertyNames)));
                }
            }

            //
            //DeletingEventArgs<T> args = new DeletingEventArgs<T>(executeCommand.AggregNode, executeCommand.Entity, executeCommand.Schema, executeCommand.Path, executeCommand.Aggreg)
            //{
            //    Refetched = refetched
            //};
            //OnDeleting(args);
            OnDeleting(executeCommand.AggregNode, executeCommand.Entity, executeCommand.Schema, executeCommand.Path, executeCommand.Aggreg, refetched, out IList<SQLStatment> before);

            //foreach (SQLStatment statment in args.Before)
            foreach (SQLStatment statment in before)
            {
                int i = Dbase.ExecuteSqlCommand(statment.Sql, statment.Parameters);
            }

            string sql = ModificationGenerator.GenerateDeleteStatement(executeCommand.PropertyValues, executeCommand.EntitySchema,
                executeCommand.UniqueKeySchema, executeCommand.ConcurrencySchema, out IReadOnlyDictionary<string, object> dbParameterValues);
            DbParameter[] dbParameters = Dbase.CreateParameters(dbParameterValues);
            int affected = Dbase.ExecuteSqlCommand(sql, dbParameters);

            if (affected > 1) throw new SQLStatmentException(string.Format(ErrorMessages.MultipleRowsAffected, affected), sql, dbParameters);
            if (affected == 0 && executeCommand.ConcurrencySchema != null)
            {
                throw new OptimisticConcurrencyException(string.Format(ErrorMessages.OptimisticConcurrencyException,
                    executeCommand.Entity, GetKeyValueMessage(executeCommand)), sql, dbParameters);
            }

            return affected;
        }

        internal protected int Execute(UpdateCommand<T> executeCommand, Modifier<T> modifier)
        {
            OnUpdating(executeCommand.AggregNode, executeCommand.Entity, executeCommand.Schema, executeCommand.Path, executeCommand.Aggreg,
                () => FetchSingleFromDb(executeCommand), out IList<SQLStatment> before, out IList<SQLStatment> after);

            // synchronize propertyValues with modified aggregNode OnUpdating
            SynchronizePropertyValues(executeCommand, modifier);

            foreach (KeyValuePair<string, object> pair in executeCommand.FixedUpdatePropertyValues)
            {
                string propertyName = pair.Key;
                object propertyValue = pair.Value;
                if (executeCommand.PropertyValues.ContainsKey(propertyName))
                {
                    object value = executeCommand.PropertyValues[propertyName];
                    if (object.Equals(propertyValue, value)) continue;

                    throw new ConstraintException(string.Format(ErrorMessages.NotChangeFixedValue, propertyName, executeCommand.Entity));
                }
            }

            // validate
            modifier.Validate(executeCommand);

            //
            foreach (SQLStatment statment in before)
            {
                int i = Dbase.ExecuteSqlCommand(statment.Sql, statment.Parameters);
            }

            //
            Dictionary<string, object> updatePropertyValues = new Dictionary<string, object>(executeCommand.FixedUpdatePropertyValues);
            foreach (KeyValuePair<string, object> propertyValue in executeCommand.PropertyValues)
            {
                string property = propertyValue.Key;
                object value = propertyValue.Value;

                if (executeCommand.FixedUpdatePropertyValues.ContainsKey(property)) continue;
                if (executeCommand.UniqueKeySchema.Elements(SchemaVocab.Property).Any(p => p.Attribute(SchemaVocab.Name).Value == property)) continue;
                if (executeCommand.EntitySchema.Elements(SchemaVocab.Property).Any(p => p.Attribute(SchemaVocab.Name).Value == property &&
                    p.Attribute(SchemaVocab.Readonly) != null && p.Attribute(SchemaVocab.Readonly).Value == "true")) continue;

                updatePropertyValues.Add(propertyValue.Key, propertyValue.Value);
            }

            //
            string sql = ModificationGenerator.GenerateUpdateStatement(executeCommand.PropertyValues, updatePropertyValues,
                executeCommand.OriginalConcurrencyCheckPropertyValues,
                executeCommand.EntitySchema, executeCommand.UniqueKeySchema, executeCommand.ConcurrencySchema,
                out IReadOnlyDictionary<string, object> dbParameterValues);
            DbParameter[] dbParameters = Dbase.CreateParameters(dbParameterValues);
            int affected = Dbase.ExecuteSqlCommand(sql, dbParameters);

            if (affected > 1) throw new SQLStatmentException(string.Format(ErrorMessages.MultipleRowsAffected, affected), sql, dbParameters);
            if (affected == 0)
            {
                if (executeCommand.ConcurrencySchema == null)
                {
                    throw new SQLStatmentException(ErrorMessages.DeletedByAnotherUser, sql, dbParameters);
                }
                else
                {
                    throw new OptimisticConcurrencyException(string.Format(ErrorMessages.OptimisticConcurrencyException,
                          executeCommand.Entity, GetKeyValueMessage(executeCommand)), sql, dbParameters);
                }
            }

            //
            foreach (SQLStatment statment in after)
            {
                int i = Dbase.ExecuteSqlCommand(statment.Sql, statment.Parameters);
            }

            return affected;
        }

        private void SynchronizePropertyValues(ExecuteCommand<T> executeCommand, Modifier<T> modifier)
        {
            // Assert(executeCommand.GetType() == typeof(InsertCommand<T>) || executeCommand.GetType() == typeof(UpdateCommand<T>));

            Dictionary<string, object> dict = modifier.GetPropertyValues(executeCommand.AggregNode, executeCommand.Entity, executeCommand.Schema);
            foreach (KeyValuePair<string, object> pair in dict)
            {
                string propertyName = pair.Key;
                object propertyvalue = pair.Value;

                if (executeCommand.PropertyValues.ContainsKey(propertyName))
                {
                    object value = executeCommand.PropertyValues[propertyName];

                    //
                    XElement uniqueKeyPropertySchema = executeCommand.UniqueKeySchema.Elements(SchemaVocab.Property)
                        .FirstOrDefault(p => p.Attribute(SchemaVocab.Name).Value == propertyName);
                    if (uniqueKeyPropertySchema != null)
                    {
                        if (value != null)
                        {
                            if (object.Equals(propertyvalue, value)) continue;

                            throw new ConstraintException(string.Format(ErrorMessages.NotChangeUniqueKeyValue, propertyName, executeCommand.Entity));
                        }
                    }

                    //
                    XElement propertySchema = executeCommand.EntitySchema.Elements(SchemaVocab.Property)
                       .First(p => p.Attribute(SchemaVocab.Name).Value == propertyName);
                    if (propertySchema.Attribute(SchemaVocab.Readonly) != null && propertySchema.Attribute(SchemaVocab.Readonly).Value == "true")
                    {
                        if (object.Equals(propertyvalue, value)) continue;

                        throw new ConstraintException(string.Format(ErrorMessages.NotChangeReadonlyValue, propertyName, executeCommand.Entity));
                    }

                    executeCommand.PropertyValues[propertyName] = propertyvalue;
                }
                else
                {
                    executeCommand.PropertyValues.Add(propertyName, propertyvalue);
                }
            }
        }

        private string GetKeyValueMessage(ExecuteCommand<T> executeCommand)
        {
            // Assert(executeCommand.GetType() == typeof(DeleteCommand<T>) || executeCommand.GetType() == typeof(UpdateCommand<T>));

            List<string> strings = new List<string>();
            foreach (XElement propertySchema in executeCommand.UniqueKeySchema.Elements(SchemaVocab.Property))
            {
                string propertyName = propertySchema.Attribute(SchemaVocab.Name).Value;
                strings.Add("'" + executeCommand.PropertyValues[propertyName].ToString() + "'");
            }
            return string.Join(",", strings);
        }

        private Dictionary<string, object> FetchSingleFromDb(ExecuteCommand<T> executeCommand)
        {
            // Assert(executeCommand.GetType() == typeof(DeleteCommand<T>) || executeCommand.GetType() == typeof(UpdateCommand<T>));

            IEnumerable<Dictionary<string, object>> refetchedRecords = FetchFromDb(executeCommand.PropertyValues, executeCommand.EntitySchema, executeCommand.UniqueKeySchema);
            int count = refetchedRecords.Count();
            if (count > 1) throw new ConstraintException(string.Format(ErrorMessages.MultipleRowsFound, count, executeCommand.Entity, GetKeyValueMessage(executeCommand)));

            return refetchedRecords.FirstOrDefault();
        }

        protected virtual IEnumerable<Dictionary<string, object>> FetchFromDb(Dictionary<string, object> propertyValues,
            XElement entitySchema, XElement keySchema)
        {
            string sql = ModificationGenerator.GenerateFindStatement(propertyValues, entitySchema, keySchema,
                out IReadOnlyDictionary<string, object> dbParameterValues);
            DbParameter[] dbParameters = Dbase.CreateParameters(dbParameterValues);
            DataTable table = Dbase.ExecuteDataTable(sql, dbParameters);

            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            foreach (DataRow row in table.Rows)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                foreach (DataColumn column in table.Columns)
                {
                    XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).First(p => p.Attribute(SchemaVocab.Column).Value == column.ColumnName);
                    string propertyName = propertySchema.Attribute(SchemaVocab.Name).Value;
                    dict.Add(propertyName, row[column]);
                }
                list.Add(dict);
            }

            return list;
        }

        protected virtual bool IsExistsInDb(Dictionary<string, object> propertyValues, XElement entitySchema, XElement keySchema)
        {
            string sql = ModificationGenerator.GenerateIsExistsStatement(propertyValues, entitySchema, keySchema,
                 out IReadOnlyDictionary<string, object> dbParameterValues);
            DbParameter[] dbParameters = Dbase.CreateParameters(dbParameterValues);
            object obj = Dbase.ExecuteScalar(sql, dbParameters);
            return obj != null;
        }

        //
        internal protected int Execute(UpdateCommandNode<T> node, Modifier<T> modifier)
        {
            List<DeleteCommand<T>> deleteCommands = new List<DeleteCommand<T>>();
            int affected = Execute(node, deleteCommands, modifier);

            deleteCommands.Reverse();
            foreach (DeleteCommand<T> deleteCommand in deleteCommands)
            {
                Execute(deleteCommand, modifier);
            }

            return affected;
        }

        private int Execute(UpdateCommandNode<T> node, List<DeleteCommand<T>> deleteCommands, Modifier<T> modifier)
        {
            int affected = Execute(node as UpdateCommand<T>, modifier);
            if (node.ChildrenCollection.Count == 0) return affected;

            //
            Dictionary<string, object> propertyValues = FetchFromDb(node.PropertyValues, node.EntitySchema, node.UniqueKeySchema).First();

            foreach (UpdateCommandNodeChildren<T> nodeChildren in node.ChildrenCollection)
            {
                DirectRelationship relationship = nodeChildren.ParentRelationship;
                string childrenPath = nodeChildren.Path;
                ICollection<UpdateCommandNode<T>> childNodes = nodeChildren.UpdateCommandNodes;

                Dictionary<string, object> relatedPropertyValues = GetRelatedPropertyValues(relationship, propertyValues);

                // establishing relationship
                foreach (UpdateCommandNode<T> childNode in childNodes)
                {
                    foreach (KeyValuePair<string, object> propertyValue in relatedPropertyValues)
                    {
                        if (!childNode.FixedUpdatePropertyValues.ContainsKey(propertyValue.Key))
                        {
                            childNode.FixedUpdatePropertyValues.Add(propertyValue.Key, propertyValue.Value);
                        }
                    }
                }

                IEnumerable<IReadOnlyDictionary<string, object>> relatedRecords = FetchRelatedCommands(
                    relatedPropertyValues, relationship.RelatedEntity, node.Schema);

                // route                
                List<IReadOnlyDictionary<string, object>> refetchedChildren = new List<IReadOnlyDictionary<string, object>>(relatedRecords);

                string childEntity = relationship.RelatedEntity;
                XElement childEntitySchema = node.Schema.GetEntitySchema(childEntity);
                XElement childKeySchema = SchemaHelper.GetKeySchema(childEntitySchema);
                XElement childConcurrencySchema = SchemaHelper.GetConcurrencySchema(childEntitySchema);
                IEnumerable<DirectRelationship> childRelationships = node.Schema.GetDirectRelationships(childEntity);

                List<UpdateCommandNode<T>> childUpdateCommandNodes = new List<UpdateCommandNode<T>>();
                foreach (UpdateCommandNode<T> childNode in childNodes)
                {
                    Dictionary<string, object> childKeyPropertyValues = new Dictionary<string, object>();
                    foreach (XElement childPropertySchema in childNode.UniqueKeySchema.Elements(SchemaVocab.Property))
                    {
                        string childPropertyName = childPropertySchema.Attribute(SchemaVocab.Name).Value;
                        object value = (childNode.PropertyValues.ContainsKey(childPropertyName)) ? childNode.PropertyValues[childPropertyName] : null;
                        childKeyPropertyValues.Add(childPropertyName, value);
                    }

                    IReadOnlyDictionary<string, object> found = Find(refetchedChildren, childKeyPropertyValues);
                    if (found == null)
                    {
                        Insert(childNode, relationship, propertyValues, modifier);
                    }
                    else
                    {
                        childUpdateCommandNodes.Add(childNode);
                        refetchedChildren.Remove(found);
                    }
                }

                //
                int index = -1;
                foreach (Dictionary<string, object> childPropertyValues in refetchedChildren)
                {
                    T aggregNode = modifier.CreateObject(childPropertyValues, childEntity);
                    DeleteCommand<T> deleteCommand = new DeleteCommand<T>(aggregNode, childEntity, node.Schema, node.Aggreg)
                    {
                        EntitySchema = childEntitySchema,
                        UniqueKeySchema = childKeySchema,
                        ConcurrencySchema = childConcurrencySchema,
                        ChildRelationships = childRelationships,
                        PropertyValues = childPropertyValues,
                        Path = string.Format("{0}[{1}]", childrenPath, index)
                    };

                    deleteCommands.Add(deleteCommand);

                    index--;
                }

                //
                foreach (UpdateCommandNode<T> childUpdateCommandNode in childUpdateCommandNodes)
                {
                    Execute(childUpdateCommandNode, deleteCommands, modifier);
                }
            }

            return affected;
        }

        private Dictionary<string, object> GetRelatedPropertyValues(DirectRelationship relationship, IReadOnlyDictionary<string, object> parentPropertyValues)
        {
            Dictionary<string, object> relatedPropertyValues = new Dictionary<string, object>();
            for (int i = 0; i < relationship.Properties.Length; i++)
            {
                string propertyName = relationship.Properties[i];
                string relatedPropertyName = relationship.RelatedProperties[i];
                relatedPropertyValues.Add(relatedPropertyName, parentPropertyValues[propertyName]);
            }

            return relatedPropertyValues;
        }

        protected virtual IEnumerable<IReadOnlyDictionary<string, object>> FetchRelatedCommands(Dictionary<string, object> relatedPropertyValues,
            string relatedEntity, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(relatedEntity);
            XElement keySchema = new XElement(entitySchema);
            keySchema.RemoveNodes();
            foreach (KeyValuePair<string, object> propertyValue in relatedPropertyValues)
            {
                string propertyName = propertyValue.Key;
                XElement keyPropertySchema = entitySchema.Elements(SchemaVocab.Property).First(p => p.Attribute(SchemaVocab.Name).Value == propertyName);
                keySchema.Add(keyPropertySchema);
            }

            return FetchFromDb(relatedPropertyValues, entitySchema, keySchema);
        }

        private IReadOnlyDictionary<string, object> Find(IEnumerable<IReadOnlyDictionary<string, object>> refetched, Dictionary<string, object> keyPropertyValues)
        {
            IEnumerable<IReadOnlyDictionary<string, object>> result = refetched;
            foreach (KeyValuePair<string, object> pair in keyPropertyValues)
            {
                result = refetched.Where(p => pair.Value != null && p[pair.Key].ToString() == pair.Value.ToString());
            }
            return result.FirstOrDefault();
        }

        private void Insert(UpdateCommandNode<T> node, DirectRelationship relationship, Dictionary<string, object> parentPropertyValues, Modifier<T> modifier)
        {
            InsertCommand<T> insertCommand = new InsertCommand<T>(node.AggregNode, node.Entity, node.Schema, node.Aggreg)
            {
                EntitySchema = node.EntitySchema,
                UniqueKeySchema = node.UniqueKeySchema,
                PropertyValues = node.PropertyValues,
                Path = node.Path,
                ParentPropertyValues = parentPropertyValues,
                ParentRelationship = relationship
            };

            SchemaHelper.SetDefaultValues(insertCommand.PropertyValues, insertCommand.EntitySchema);

            Execute(insertCommand, modifier);

            foreach (UpdateCommandNodeChildren<T> nodeChildren in node.ChildrenCollection)
            {
                DirectRelationship childRelationship = nodeChildren.ParentRelationship;
                ICollection<UpdateCommandNode<T>> childNodes = nodeChildren.UpdateCommandNodes;

                foreach (UpdateCommandNode<T> childNode in childNodes)
                {
                    Insert(childNode, childRelationship, insertCommand.PropertyValues, modifier);
                }
            }
        }


    }
}
