using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Modification
{
    // Modifier.Check.cs
    public abstract partial class Modifier<T>
    {
        protected void CheckConstraints()
        {
            foreach (ExecuteAggregation<T> executeAggregation in ExecuteAggregations)
            {
                foreach (ExecuteCommand<T> executeCommand in executeAggregation.ExecuteCommands)
                {
                    if (executeCommand is UpdateCommandNode<T>)
                    {
                        CheckConstraints(executeCommand as UpdateCommandNode<T>);
                    }
                    else
                    {
                        if (executeCommand is InsertCommand<T>)
                        {
                            CheckConstraints(executeCommand as InsertCommand<T>);
                        }
                        else if (executeCommand is DeleteCommand<T>)
                        {
                            CheckConstraints(executeCommand as DeleteCommand<T>);
                        }
                        else if (executeCommand is UpdateCommand<T>) // DeleteAggregation SetNull
                        {
                            CheckConstraints(executeCommand as UpdateCommand<T>);
                        }
                    }
                }
            }
        }

        //
        protected void CheckConstraints(UpdateCommandNode<T> node)
        {
            CheckRelationshipConstraint(node);

            foreach (UpdateCommandNodeChildren<T> nodeChildren in node.ChildrenCollection)
            {
                foreach (UpdateCommandNode<T> childNode in nodeChildren.UpdateCommandNodes)
                {
                    CheckConstraints(childNode);
                }
            }
        }

        // in Execute(InsertCommand<T>, Modifier<T>),Database.Modification.cs
        internal void CheckConstraints(InsertCommand<T> insertCommand)
        {
            CheckAutoIncrementConstraint(insertCommand);
            CheckReadOnlyConstraint(insertCommand);
            CheckRelationshipConstraint(insertCommand);
        }

        protected void CheckConstraints(DeleteCommand<T> deleteCommand)
        {
            CheckConcurrencyCheckConstraint(deleteCommand);
            CheckRelationshipConstraint(deleteCommand);
        }

        // in Execute(UpdateCommand<T>, Modifier<T>),Database.Modification.cs
        internal void CheckConstraints(UpdateCommand<T> updateCommand)
        {
            if (updateCommand.ConcurrencySchema != null)
            {
                if (updateCommand.OriginalConcurrencyCheckPropertyValues == null)
                {
                    throw new ConstraintException(string.Format(ErrorMessages.Constraint_OriginalConcurrencyCheckRequierd, updateCommand.Entity));
                }
                else
                {
                    CheckConcurrencyCheckConstraint(updateCommand);
                }

                CheckRelationshipConstraint(updateCommand);
            }
        }

        protected void CheckAutoIncrementConstraint(InsertCommand<T> insertCommand)
        {
            XElement autoPropertySchema = insertCommand.EntitySchema.Elements(SchemaVocab.Property).FirstOrDefault(p =>
                p.Attribute(SchemaVocab.AutoIncrement) != null && p.Attribute(SchemaVocab.AutoIncrement).Value == "true");
            if (autoPropertySchema == null) return;

            string propertyName = autoPropertySchema.Attribute(SchemaVocab.Name).Value;
            if (GetValue(insertCommand.PropertyValues, propertyName) != null)
            {
                string errorMessage = string.Format(ErrorMessages.Constraint_InsertExplicitAutoIncrement, propertyName, insertCommand.Entity);
                throw new ConstraintException(errorMessage);
            }
        }

        protected void CheckReadOnlyConstraint(InsertCommand<T> insertCommand)
        {
            IEnumerable<XElement> propertySchemas = insertCommand.EntitySchema.Elements(SchemaVocab.Property).Where(p =>
                p.Attribute(SchemaVocab.Readonly) != null && p.Attribute(SchemaVocab.Readonly).Value == "true");
            foreach (XElement propertySchema in propertySchemas)
            {
                string propertyName = propertySchema.Attribute(SchemaVocab.Name).Value;
                if (GetValue(insertCommand.PropertyValues, propertyName) != null)
                {
                    string errorMessage = string.Format(ErrorMessages.Constraint_InsertExplicitReadonly, propertyName, insertCommand.Entity);
                    throw new ConstraintException(errorMessage);
                }
            }
        }

        protected void CheckRelationshipConstraint(ExecuteCommand<T> executeCommand)
        {
            if (executeCommand.ParentRelationship == null) return;

            string parentEntity = executeCommand.ParentRelationship.Entity;
            for (int i = 0; i < executeCommand.ParentRelationship.Properties.Length; i++)
            {
                string parentProperty = executeCommand.ParentRelationship.Properties[i];
                object parentValue = GetValue(executeCommand.ParentPropertyValues, parentProperty);

                string property = executeCommand.ParentRelationship.RelatedProperties[i];
                object value = GetValue(executeCommand.PropertyValues, property);
                if (value == null) continue;

                if (object.Equals(parentValue, value)) continue;

                string errorMessage = string.Format(ErrorMessages.Constraint_RelationshipConflicted,
                    executeCommand.Entity, property + ":" + value.ToString(),
                    parentEntity, parentProperty + ":" + ((parentValue == null) ? "null" : parentValue.ToString()));
                throw new ConstraintException(errorMessage);
            }
        }

        // overload
        protected void CheckConcurrencyCheckConstraint(DeleteCommand<T> deleteCommand)
        {
            CheckConcurrencyCheckConstraint(deleteCommand.PropertyValues, deleteCommand.ConcurrencySchema, deleteCommand);
        }

        // overload
        protected void CheckConcurrencyCheckConstraint(UpdateCommand<T> updateCommand)
        {
            CheckConcurrencyCheckConstraint(updateCommand.OriginalConcurrencyCheckPropertyValues, updateCommand.ConcurrencySchema, updateCommand);
        }

        protected void CheckConcurrencyCheckConstraint(Dictionary<string, object> propertyValues,
            XElement concurrencySchema, ExecuteCommand<T> executeCommand)
        {
            // Assert(executeCommand.GetType() == typeof(DeleteCommand<T>) || executeCommand.GetType() == typeof(UpdateCommand<T>));

            if (concurrencySchema == null) return;

            List<string> errors = new List<string>();

            foreach (XElement propertySchema in concurrencySchema.Elements())
            {
                string propertyName = propertySchema.Attribute(SchemaVocab.Name).Value;
                if (propertyValues.ContainsKey(propertyName))
                {
                    if (propertyValues[propertyName] == null)
                    {
                        errors.Add("'" + propertyName + "'");
                    }
                }
                else
                {
                    errors.Add("'" + propertyName + "'");
                }
            }

            if (errors.Count == 0) return;

            string errorMessage = string.Format(ErrorMessages.Constraint_IncompleteConcurrencyCheck, string.Join(",", errors));
            throw new ConstraintException(errorMessage);
        }


    }
}
