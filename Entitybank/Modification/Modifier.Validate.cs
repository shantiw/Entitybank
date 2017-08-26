using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.Modification
{
    // Modifier.Validate.cs
    public abstract partial class Modifier<T>
    {
        //public event ValidatingEventHandler<T> Validating;

        //protected virtual void OnValidating(ValidatingEventArgs<T> args)
        //{
        //    Validating?.Invoke(this, args);
        //}
        protected abstract void OnValidating(Execution execution, T aggreg, string entity, XElement schema,
            IEnumerable<ExecutionEntry<T>> context, out ICollection<ValidationResult> validationResults);

        public void Validate()
        {
            ICollection<ValidationResult>[] validationResultCollections = GetValidationResultCollections();
            TryThrowValidationException(validationResultCollections);
        }

        private void TryThrowValidationException(ICollection<ValidationResult>[] validationResultCollections)
        {
            if (validationResultCollections.Length > 0)
            {
                ValidationResult first = validationResultCollections[0].First();
                throw new ValidationException(first, null, validationResultCollections);
            }
        }

        public ICollection<ValidationResult>[] GetValidationResultCollections()
        {
            List<ICollection<ValidationResult>> list = new List<ICollection<ValidationResult>>();

            List<ExecutionEntry<T>> context = new List<ExecutionEntry<T>>();
            foreach (ExecuteAggregation<T> executeAggregation in ExecuteAggregations)
            {
                Execution execution = GetExecution(executeAggregation);
                context.Add(new ExecutionEntry<T>(execution, executeAggregation.Aggreg, executeAggregation.Entity, executeAggregation.Schema));
            }

            foreach (ExecuteAggregation<T> executeAggregation in ExecuteAggregations)
            {
                List<ValidationResult> validationResults = new List<ValidationResult>();

                foreach (ExecuteCommand<T> executeCommand in executeAggregation.ExecuteCommands)
                {
                    if (executeCommand is UpdateCommandNode<T>)
                    {
                        validationResults.AddRange(GetValidationResults(executeCommand as UpdateCommandNode<T>));
                    }
                    else
                    {
                        if (executeCommand is InsertCommand<T>)
                        {
                            validationResults.AddRange(GetValidationResults(executeCommand as InsertCommand<T>));
                        }
                        else if (executeCommand is DeleteCommand<T>)
                        {
                            validationResults.AddRange(GetValidationResults(executeCommand as DeleteCommand<T>));
                        }
                        else if (executeCommand is UpdateCommand<T>) // DeleteAggregation SetNull
                        {
                            validationResults.AddRange(GetValidationResults(executeCommand as UpdateCommand<T>));
                        }
                    }
                }

                Execution execution = GetExecution(executeAggregation);
                OnValidating(execution, executeAggregation.Aggreg, executeAggregation.Entity, executeAggregation.Schema, context, out ICollection<ValidationResult> results);

                validationResults.AddRange(results.Where(r => r != ValidationResult.Success));

                if (validationResults.Count > 0) list.Add(validationResults);
            }

            return list.ToArray();
        }

        private Execution GetExecution(ExecuteAggregation<T> executeAggregation)
        {
            Execution execution;
            if (executeAggregation is CreateAggregation<T>)
            {
                execution = Execution.Create;
            }
            else if (executeAggregation is DeleteAggregation<T>)
            {
                execution = Execution.Delete;
            }
            else //if (executeAggregation is UpdateAggregation<T>)
            {
                execution = Execution.Update;
            }
            return execution;
        }

        internal protected virtual List<ValidationResult> GetValidationResults(InsertCommand<T> insertCommand)
        {
            List<ValidationResult> validationResults = new List<ValidationResult>();

            ValidationResult validationResult = GetAutoIncrementValidationResult(insertCommand);
            if (validationResult != null) validationResults.Add(validationResult);

            validationResults.AddRange(GetReadOnlyValidationResults(insertCommand));

            validationResult = GetRelationshipValidationResult(insertCommand);
            if (validationResult != null) validationResults.Add(validationResult);

            validationResults.AddRange(GetAnnotationValidationResults(insertCommand));
            return validationResults;
        }

        protected virtual List<ValidationResult> GetValidationResults(DeleteCommand<T> deleteCommand)
        {
            List<ValidationResult> validationResults = new List<ValidationResult>();

            ValidationResult validationResult = GetConcurrencyCheckValidationResult(deleteCommand);
            if (validationResult != null) validationResults.Add(validationResult);

            validationResult = GetRelationshipValidationResult(deleteCommand);
            if (validationResult != null) validationResults.Add(validationResult);

            return validationResults;
        }

        internal protected virtual List<ValidationResult> GetValidationResults(UpdateCommand<T> updateCommand)
        {
            List<ValidationResult> validationResults = new List<ValidationResult>();

            if (updateCommand.ConcurrencySchema != null)
            {
                if (updateCommand.OriginalConcurrencyCheckPropertyValues == null)
                {
                    validationResults.Add(new ValidationResult(ErrorMessages.Validate_OriginalConcurrencyCheckRequierd,
                        new List<string>() { updateCommand.Path }));
                }
                else
                {
                    ValidationResult result = GetConcurrencyCheckValidationResult(updateCommand);
                    if (result != null) validationResults.Add(result);
                }
            }

            ValidationResult validationResult = GetRelationshipValidationResult(updateCommand);
            if (validationResult != null) validationResults.Add(validationResult);

            validationResults.AddRange(GetAnnotationValidationResults(updateCommand));
            return validationResults;
        }

        protected ValidationResult GetAutoIncrementValidationResult(InsertCommand<T> insertCommand)
        {
            XElement autoPropertySchema = insertCommand.EntitySchema.Elements(SchemaVocab.Property).FirstOrDefault(p =>
                p.Attribute(SchemaVocab.AutoIncrement) != null && p.Attribute(SchemaVocab.AutoIncrement).Value == "true");
            if (autoPropertySchema == null) return null;

            string propertyName = autoPropertySchema.Attribute(SchemaVocab.Name).Value;
            if (GetValue(insertCommand.PropertyValues, propertyName) != null)
            {
                string errorMessage = string.Format(ErrorMessages.Validate_InsertExplicitAutoIncrement, propertyName, insertCommand.Entity);
                return new ValidationResult(errorMessage, new List<string>() { insertCommand.Path });
            }

            return null;
        }

        protected List<ValidationResult> GetReadOnlyValidationResults(InsertCommand<T> insertCommand)
        {
            List<ValidationResult> validationResults = new List<ValidationResult>();

            IEnumerable<XElement> propertySchemas = insertCommand.EntitySchema.Elements(SchemaVocab.Property).Where(p =>
                p.Attribute(SchemaVocab.Readonly) != null && p.Attribute(SchemaVocab.Readonly).Value == "true");
            foreach (XElement propertySchema in propertySchemas)
            {
                string propertyName = propertySchema.Attribute(SchemaVocab.Name).Value;
                if (GetValue(insertCommand.PropertyValues, propertyName) != null)
                {
                    string errorMessage = string.Format(ErrorMessages.Validate_InsertExplicitReadonly, propertyName, insertCommand.Entity);
                    ValidationResult validationResult = new ValidationResult(errorMessage, new List<string>() { insertCommand.Path });
                    validationResults.Add(validationResult);
                }
            }

            return validationResults;
        }

        // overload
        protected ValidationResult GetConcurrencyCheckValidationResult(DeleteCommand<T> deleteCommand)
        {
            return GetConcurrencyCheckValidationResult(deleteCommand.PropertyValues, deleteCommand.ConcurrencySchema, deleteCommand);
        }

        // overload
        protected ValidationResult GetConcurrencyCheckValidationResult(UpdateCommand<T> updateCommand)
        {
            return GetConcurrencyCheckValidationResult(updateCommand.OriginalConcurrencyCheckPropertyValues, updateCommand.ConcurrencySchema, updateCommand);
        }

        protected ValidationResult GetConcurrencyCheckValidationResult(Dictionary<string, object> propertyValues,
            XElement concurrencySchema, ExecuteCommand<T> executeCommand)
        {
            // Assert(executeCommand.GetType() == typeof(DeleteCommand<T>) || executeCommand.GetType() == typeof(UpdateCommand<T>));

            if (concurrencySchema == null) return null;

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

            if (errors.Count == 0) return null;

            string errorMessage = string.Format(ErrorMessages.Validate_IncompleteConcurrencyCheck, string.Join(",", errors));
            return new ValidationResult(errorMessage, new List<string>() { executeCommand.Path });
        }

        protected ValidationResult GetRelationshipValidationResult(ExecuteCommand<T> executeCommand)
        {
            if (executeCommand.ParentRelationship == null) return null;

            string parentEntity = executeCommand.ParentRelationship.Entity;
            for (int i = 0; i < executeCommand.ParentRelationship.Properties.Length; i++)
            {
                string parentProperty = executeCommand.ParentRelationship.Properties[i];
                object parentValue = GetValue(executeCommand.ParentPropertyValues, parentProperty);

                string property = executeCommand.ParentRelationship.RelatedProperties[i];
                object value = GetValue(executeCommand.PropertyValues, property);
                if (value == null) continue;

                if (object.Equals(parentValue, value)) continue;

                string errorMessage = string.Format(ErrorMessages.Validate_RelationshipConflicted,
                    executeCommand.Entity, property + ":" + value.ToString(),
                    parentEntity, parentProperty + ":" + ((parentValue == null) ? "null" : parentValue.ToString()));

                return new ValidationResult(errorMessage, new List<string>() { executeCommand.Path });
            }

            return null;
        }

        protected object GetValue(Dictionary<string, object> propertyValues, string property)
        {
            if (propertyValues.ContainsKey(property))
            {
                return propertyValues[property];
            }
            return null;
        }

        protected List<ValidationResult> GetAnnotationValidationResults(ExecuteCommand<T> executeCommand)
        {
            // Assert(executeCommand.GetType() == typeof(InsertCommand<T>) || executeCommand.GetType() == typeof(UpdateCommand<T>));

            List<ValidationResult> validationResults = new List<ValidationResult>();

            XElement entitySchema = executeCommand.EntitySchema;
            foreach (KeyValuePair<string, object> pair in executeCommand.PropertyValues)
            {
                string propertyName = pair.Key;
                object propertyValue = pair.Value;
                XElement propertySchema = SchemaHelper.GetPropertySchema(entitySchema, propertyName);
                if (propertySchema == null) continue;
                if (propertySchema.Attribute(SchemaVocab.Column) == null) continue;

                foreach (ValidationAttribute validationAttribute in SchemaHelper.CreateValidationAttributes(propertySchema))
                {
                    if (!validationAttribute.IsValid(propertyValue))
                    {
                        string dispalyName = SchemaHelper.GetDisplayName(propertySchema);
                        string errorMessage = validationAttribute.FormatErrorMessage(dispalyName);
                        ValidationResult validationResult = new ValidationResult(errorMessage, new List<string>() { executeCommand.Path });
                        validationResults.Add(validationResult);
                    }
                }
            }

            return validationResults;
        }

        //
        protected virtual List<ValidationResult> GetValidationResults(UpdateCommandNode<T> updateCommandNode)
        {
            List<ValidationResult> validationResults = new List<ValidationResult>();

            FillValidationResults(updateCommandNode, validationResults);

            return validationResults;
        }

        private void FillValidationResults(UpdateCommandNode<T> node, List<ValidationResult> validationResults)
        {
            ValidationResult validationResult = GetRelationshipValidationResult(node);
            if (validationResult != null) validationResults.Add(validationResult);

            validationResults.AddRange(GetAnnotationValidationResults(node));

            foreach (UpdateCommandNodeChildren<T> nodeChildren in node.ChildrenCollection)
            {
                foreach (UpdateCommandNode<T> childNode in nodeChildren.UpdateCommandNodes)
                {
                    FillValidationResults(childNode, validationResults);
                }
            }
        }


    }
}
