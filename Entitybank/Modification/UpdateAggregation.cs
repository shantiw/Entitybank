using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Modification
{
    public abstract class UpdateAggregation<T> : ExecuteAggregation<T>
    {
        public UpdateAggregation(T aggreg, string entity, XElement schema) : base(aggreg, entity, schema)
        {
            XElement entitySchema = GetEntitySchema(entity);
            UpdateCommandNode<T> node = Split(aggreg, entity, entitySchema, GetKeySchema(entitySchema), GetConcurrencySchema(entitySchema), null, null, "/");
            Commands.Add(node);
        }

        protected UpdateCommandNode<T> Split(T obj, string entity, XElement entitySchema, XElement uniqueKeySchema, XElement concurrencySchema,
            DirectRelationship parentRelationship, Dictionary<string, object> parentPropertyValues, string path)
        {
            UpdateCommandNode<T> executeCommand = CreateUpdateCommandNode(obj, entity);

            executeCommand.EntitySchema = entitySchema;
            executeCommand.UniqueKeySchema = uniqueKeySchema;
            executeCommand.ConcurrencySchema = concurrencySchema;
            executeCommand.ParentPropertyValues = parentPropertyValues; // validate
            executeCommand.ParentRelationship = parentRelationship; // validate
            executeCommand.Path = path;

            executeCommand.PropertyValues = GetPropertyValues(executeCommand.AggregNode, executeCommand.EntitySchema);

            //executeCommand.FixedUpdatePropertyValues = new Dictionary<string, object>();

            T original = GetOriginal(obj);
            if (original == null)
            {
                executeCommand.OriginalConcurrencyCheckPropertyValues = null;
            }
            else
            {
                executeCommand.OriginalConcurrencyCheckPropertyValues = GetPropertyValues(original, GetEntitySchema(entity));
            }

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

                //
                UpdateCommandNodeChildren<T> nodeChildren = new UpdateCommandNodeChildren<T>(childRelationship.DirectRelationships[0], path);
                executeCommand.ChildrenCollection.Add(nodeChildren);

                if (childRelationship is ManyToManyRelationship)
                {
                    Split(children, childRelationship as ManyToManyRelationship, executeCommand.PropertyValues, path, nodeChildren.UpdateCommandNodes);
                }

                //
                XElement childKeySchema = GetKeySchema(childEntitySchema);
                XElement childConcurrencySchema = GetConcurrencySchema(childEntitySchema);

                int index = 0;
                foreach (T child in children)
                {
                    UpdateCommandNode<T> childNode = Split(child, childEntity, childEntitySchema, childKeySchema, childConcurrencySchema,
                        childRelationship.DirectRelationships[0], executeCommand.PropertyValues,
                        string.Format("{0}[{1}]", childPath, index));
                    nodeChildren.UpdateCommandNodes.Add(childNode);
                    index++;
                }
            }

            return executeCommand;
        }

        protected void Split(IEnumerable<T> children, ManyToManyRelationship manyToManyRelationship, Dictionary<string, object> parentPropertyValues, string path,
            ICollection<UpdateCommandNode<T>> childNodes)
        {
            XElement mmKeySchema = TransKeySchema(manyToManyRelationship, out XElement mmEntitySchema);
            string mmEntity = mmEntitySchema.Name.LocalName;
            XElement mmConcurrencySchema = GetConcurrencySchema(mmEntitySchema);

            int index = 0;
            foreach (T child in children)
            {
                Dictionary<string, object> childPropertyValues = GetPropertyValues(child, mmEntitySchema);
                Dictionary<string, object> mmPropertyValues = TransPropertyValues(manyToManyRelationship, parentPropertyValues, childPropertyValues);

                Dictionary<string, object> mmUpdatePropertyValues = new Dictionary<string, object>(mmPropertyValues);

                T mmChild = child;
                ResetObjectValues(mmChild, mmPropertyValues);

                UpdateCommandNode<T> mmExecuteCommand = CreateUpdateCommandNode(mmChild, mmEntity);

                mmExecuteCommand.EntitySchema = mmEntitySchema;
                mmExecuteCommand.UniqueKeySchema = mmKeySchema;
                mmExecuteCommand.ConcurrencySchema = mmConcurrencySchema;
                mmExecuteCommand.PropertyValues = mmPropertyValues;
                mmExecuteCommand.ParentPropertyValues = parentPropertyValues;
                mmExecuteCommand.ParentRelationship = manyToManyRelationship.DirectRelationships[0];
                mmExecuteCommand.FixedUpdatePropertyValues = mmUpdatePropertyValues;
                mmExecuteCommand.OriginalConcurrencyCheckPropertyValues = null;
                mmExecuteCommand.Path = path;

                childNodes.Add(mmExecuteCommand);

                index++;
            }
        }

        protected T GetOriginal(T obj)
        {
            return (ExecuteAggregationHelper as IUpdateAggregationHelper<T>).GetOriginal(obj);
        }

        protected UpdateCommandNode<T> CreateUpdateCommandNode(T aggregNode, string entity)
        {
            return (ExecuteAggregationHelper as IUpdateAggregationHelper<T>).CreateUpdateCommandNode(aggregNode, entity, Schema, Aggreg);
        }


    }
}
