using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Modification
{
    public abstract class DeleteAggregation<T> : ExecuteAggregation<T>
    {
        public DeleteAggregation(T aggreg, string entity, XElement schema) : base(aggreg, entity, schema)
        {
            if (aggreg == null) return; // UpdateAggregation.Original

            XElement entitySchema = GetEntitySchema(entity);
            Split(aggreg, entitySchema, null, null, "/");

            Commands.Reverse();
        }

        internal protected void Split(T obj, XElement entitySchema, DirectRelationship parentRelationship, Dictionary<string, object> parentPropertyValues, string path)
        {
            string entity = entitySchema.Attribute(SchemaVocab.Name).Value;

            DeleteCommand<T> executeCommand = CreateDeleteCommand(obj, entity);
            executeCommand.EntitySchema = entitySchema;
            executeCommand.UniqueKeySchema = GetKeySchema(entitySchema);
            executeCommand.ConcurrencySchema = GetConcurrencySchema(entitySchema);
            executeCommand.ChildRelationships = GetDirectRelationships(entity);
            executeCommand.ParentPropertyValues = parentPropertyValues;
            executeCommand.ParentRelationship = parentRelationship;
            executeCommand.Path = path;

            executeCommand.PropertyValues = GetPropertyValues(executeCommand.AggregNode, executeCommand.EntitySchema);

            Commands.Add(executeCommand);

            //
            IEnumerable<KeyValuePair<XElement, T>> propertySchemaChildrens = GetPropertySchemaChildrens(executeCommand.AggregNode, executeCommand.EntitySchema);
            foreach (KeyValuePair<XElement, T> childrenPair in propertySchemaChildrens)
            {
                XElement propertySchema = childrenPair.Key;
                IEnumerable<T> children = GetChildren(childrenPair.Value);

                //
                string childrenPath = path + propertySchema.Attribute(SchemaVocab.Name).Value;

                //
                XElement childEntitySchema = GetEntitySchemaByCollection(propertySchema.Attribute(SchemaVocab.Collection).Value);
                string childEntity = childEntitySchema.Attribute(SchemaVocab.Name).Value;

                //
                string relationshipString = propertySchema.Attribute(SchemaVocab.Relationship).Value;
                Relationship childRelationship = GetParentChildrenRelationship(relationshipString, entity, childEntity);
                if (childRelationship == null) continue;

                if (childRelationship is ManyToManyRelationship)
                {
                    Split(children, childEntitySchema, childRelationship as ManyToManyRelationship, executeCommand.PropertyValues, childrenPath);
                    return;
                }

                int index = 0;
                foreach (T child in children)
                {
                    Split(child, childEntitySchema, childRelationship.DirectRelationships[0], executeCommand.PropertyValues,
                        string.Format("{0}[{1}]", childrenPath, index));
                    index++;
                }
            }
        }

        internal protected void Split(IEnumerable<T> children, XElement childEntitySchema, ManyToManyRelationship manyToManyRelationship, Dictionary<string, object> parentPropertyValues, string childrenPath)
        {
            string childEntity = childEntitySchema.Attribute(SchemaVocab.Name).Value;

            XElement mmKeySchema = TransKeySchema(manyToManyRelationship, out XElement mmEntitySchema);
            string mmEntity = mmEntitySchema.Attribute(SchemaVocab.Name).Value;
            XElement mmConcurrencySchema = GetConcurrencySchema(mmEntitySchema);

            //
            bool mmDeleteTransSetNull = IsDeleteTransSetNull(manyToManyRelationship);

            Dictionary<string, object> mmUpdatePropertyValues = null;
            IEnumerable<DirectRelationship> mmRelationships = null;
            if (mmDeleteTransSetNull)
            {
                mmUpdatePropertyValues = new Dictionary<string, object>();
                DirectRelationship oneToManyRelationship = manyToManyRelationship.DirectRelationships[0];
                for (int i = 0; i < oneToManyRelationship.RelatedProperties.Length; i++)
                {
                    string relatedPropertyName = oneToManyRelationship.RelatedProperties[i];
                    mmUpdatePropertyValues.Add(relatedPropertyName, null);
                }
            }
            else
            {
                mmRelationships = GetDirectRelationships(mmEntity);
            }

            int index = 0;
            foreach (T child in children)
            {
                Dictionary<string, object> childPropertyValues = GetPropertyValues(child, childEntitySchema);
                Dictionary<string, object> mmPropertyValues = TransPropertyValues(manyToManyRelationship, parentPropertyValues, childPropertyValues);

                ExecuteCommand<T> mmExecuteCommand;
                if (mmDeleteTransSetNull)
                {
                    UpdateCommand<T> mmUpdateCommand = CreateUpdateCommand(child, childEntity);
                    mmUpdateCommand.FixedUpdatePropertyValues = mmUpdatePropertyValues;
                    mmUpdateCommand.ConcurrencySchema = mmConcurrencySchema;

                    mmExecuteCommand = mmUpdateCommand;
                }
                else
                {
                    DeleteCommand<T> mmDeleteCommand = CreateDeleteCommand(child, childEntity);
                    mmDeleteCommand.ConcurrencySchema = mmConcurrencySchema;
                    mmDeleteCommand.ChildRelationships = mmRelationships;

                    mmExecuteCommand = mmDeleteCommand;
                }

                mmExecuteCommand.EntitySchema = mmEntitySchema;
                mmExecuteCommand.UniqueKeySchema = mmKeySchema;
                mmExecuteCommand.PropertyValues = mmPropertyValues;
                mmExecuteCommand.ParentPropertyValues = parentPropertyValues;
                mmExecuteCommand.ParentRelationship = manyToManyRelationship.DirectRelationships[0];
                mmExecuteCommand.Path = string.Format("{0}[{1}]", childrenPath, index);

                Commands.Add(mmExecuteCommand);

                index++;
            }
        }

        protected DeleteCommand<T> CreateDeleteCommand(T aggregNode, string entity)
        {
            return (ExecuteAggregationHelper as IDeleteAggregationHelper<T>).CreateDeleteCommand(aggregNode, entity, Schema, Aggreg);
        }

        protected UpdateCommand<T> CreateUpdateCommand(T aggregNode, string entity)
        {
            return (ExecuteAggregationHelper as IDeleteAggregationHelper<T>).CreateUpdateCommand(aggregNode, entity, Schema, Aggreg);
        }


    }
}
