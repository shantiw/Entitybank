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
        public event ValidatingEventHandler<T> Validating;

        protected virtual void OnValidating(ValidatingEventArgs<T> args)
        {
            Validating?.Invoke(this, args);
        }

        public void Validate()
        {
            ICollection<ValidationResult>[] validationResultCollections = GetValidationResultCollections();
            TryThrowValidationException(validationResultCollections);
        }

        private void TryThrowValidationException(ICollection<ValidationResult>[] validationResultCollections)
        {
            ValidationException validationException = ValidationExceptionHelper.CreateValidationException(validationResultCollections);
            if (validationException == null) return;

            throw validationException;
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
                            validationResults.AddRange(GetAnnotationValidationResults(executeCommand as InsertCommand<T>));
                        }
                        else if (executeCommand is DeleteCommand<T>)
                        {
                        }
                        else if (executeCommand is UpdateCommand<T>)
                        {
                            validationResults.AddRange(GetAnnotationValidationResults(executeCommand as UpdateCommand<T>));
                        }
                    }
                }

                //
                ValidatingEventArgs<T> args = new ValidatingEventArgs<T>(GetExecution(executeAggregation), executeAggregation.Aggreg, executeAggregation.Entity, executeAggregation.Schema, context);
                OnValidating(args);

                validationResults.AddRange(args.ValidationResults.Where(r => r != ValidationResult.Success));

                if (validationResults.Count > 0) list.Add(validationResults);
            }

            return list.ToArray();
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
             validationResults.AddRange(GetAnnotationValidationResults(node));

            foreach (UpdateCommandNodeChildren<T> nodeChildren in node.ChildrenCollection)
            {
                foreach (UpdateCommandNode<T> childNode in nodeChildren.UpdateCommandNodes)
                {
                    FillValidationResults(childNode, validationResults);
                }
            }
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

                foreach (ValidationAttribute validationAttribute in propertySchema.CreateValidationAttributes())
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

        protected static Execution GetExecution(ExecuteAggregation<T> executeAggregation)
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

        protected static object GetValue(Dictionary<string, object> propertyValues, string property)
        {
            if (propertyValues.ContainsKey(property))
            {
                return propertyValues[property];
            }
            return null;
        }


    }
}
