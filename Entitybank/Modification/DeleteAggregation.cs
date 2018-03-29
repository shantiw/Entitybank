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
            XElement entitySchema = GetEntitySchema(entity);
            IEnumerable<DirectRelationship> relationships = GetDirectRelationships(entity);
            Split(aggreg, Entity, entitySchema, GetKeySchema(entitySchema), GetConcurrencySchema(entitySchema), relationships, null, null, "/");

            Commands = Commands.Reverse().ToList();
        }

        protected void Split(T obj, string entity, XElement entitySchema, XElement uniqueKeySchema, XElement concurrencySchema,
            IEnumerable<DirectRelationship> childRelationships, DirectRelationship parentRelationship, Dictionary<string, object> parentPropertyValues, string path)
        {
            DeleteCommand<T> executeCommand = CreateDeleteCommand(obj, entity);

            executeCommand.EntitySchema = entitySchema;
            executeCommand.UniqueKeySchema = uniqueKeySchema;
            executeCommand.ConcurrencySchema = concurrencySchema;
            executeCommand.ChildRelationships = childRelationships;
            executeCommand.ParentPropertyValues = parentPropertyValues;
            executeCommand.ParentRelationship = parentRelationship;
            executeCommand.Path = path;

            executeCommand.PropertyValues = GetPropertyValues(executeCommand.AggregNode, executeCommand.EntitySchema);

            Commands.Add(executeCommand);

            //
            Dictionary<XElement, T> propertySchemaChildrenDictionary = GetPropertySchemaChildrenDictionary(executeCommand.AggregNode, executeCommand.EntitySchema);
            foreach (KeyValuePair<XElement, T> childrenPair in propertySchemaChildrenDictionary)
            {
                XElement propertySchema = childrenPair.Key;
                IEnumerable<T> children = GetChildren(childrenPair.Value);

                //
                string childPath = path + propertySchema.Attribute(SchemaVocab.Name).Value;

                //
                XElement childEntitySchema = GetEntitySchemaByCollection(propertySchema.Attribute(SchemaVocab.Collection).Value);
                string childEntity = childEntitySchema.Attribute(SchemaVocab.Name).Value;

                //
                string relationshipString = propertySchema.Attribute(SchemaVocab.Relationship).Value;
                Relationship childRelationship = GetParentChildrenRelationship(relationshipString, entity, childEntity);
                if (childRelationship == null) continue;

                if (childRelationship is ManyToManyRelationship)
                {
                    Split(children, childRelationship as ManyToManyRelationship, executeCommand.PropertyValues, path);
                    return;
                }

                XElement childKeySchema = GetKeySchema(childEntitySchema);
                XElement childConcurrencySchema = GetConcurrencySchema(childEntitySchema);
                IEnumerable<DirectRelationship> childDirectRelationships = GetDirectRelationships(childEntity);

                int index = 0;
                foreach (T child in children)
                {
                    Split(child, childEntity, childEntitySchema, childKeySchema, childConcurrencySchema, childDirectRelationships,
                        childRelationship.DirectRelationships[0], executeCommand.PropertyValues,
                        string.Format("{0}[{1}]", childPath, index));
                    index++;
                }
            }
        }

        protected void Split(IEnumerable<T> children, ManyToManyRelationship manyToManyRelationship, Dictionary<string, object> parentPropertyValues, string path)
        {
            XElement mmKeySchema = TransKeySchema(manyToManyRelationship, out XElement mmEntitySchema);
            string mmEntity = mmEntitySchema.Name.LocalName;
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
                Dictionary<string, object> childPropertyValues = GetPropertyValues(child, mmEntitySchema);
                Dictionary<string, object> mmPropertyValues = TransPropertyValues(manyToManyRelationship, parentPropertyValues, childPropertyValues);

                T mmChild = child;
                ResetObjectValues(mmChild, mmPropertyValues);

                ExecuteCommand<T> mmExecuteCommand;
                if (mmDeleteTransSetNull)
                {
                    UpdateCommand<T> mmUpdateCommand = CreateUpdateCommand(mmChild, mmEntity);
                    mmUpdateCommand.FixedUpdatePropertyValues = mmUpdatePropertyValues;
                    mmUpdateCommand.ConcurrencySchema = mmConcurrencySchema;
                    mmUpdateCommand.OriginalConcurrencyCheckPropertyValues = null;

                    mmExecuteCommand = mmUpdateCommand;
                }
                else
                {
                    DeleteCommand<T> mmDeleteCommand = CreateDeleteCommand(mmChild, mmEntity);
                    mmDeleteCommand.ConcurrencySchema = mmConcurrencySchema;
                    mmDeleteCommand.ChildRelationships = mmRelationships;

                    mmExecuteCommand = mmDeleteCommand;
                }

                mmExecuteCommand.EntitySchema = mmEntitySchema;
                mmExecuteCommand.UniqueKeySchema = mmKeySchema;
                mmExecuteCommand.PropertyValues = mmPropertyValues;
                mmExecuteCommand.ParentPropertyValues = parentPropertyValues;
                mmExecuteCommand.ParentRelationship = manyToManyRelationship.DirectRelationships[0];
                mmExecuteCommand.Path = path;

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
