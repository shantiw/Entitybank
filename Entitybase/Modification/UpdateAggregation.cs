using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Modification
{
    public abstract partial class UpdateAggregation<T> : ExecuteAggregation<T>
    {
        public UpdateAggregation(T aggreg, string entity, XElement schema) : base(aggreg, entity, schema)
        {
            XElement entitySchema = GetEntitySchema(entity);
            UpdateCommandNode<T> node = Split(aggreg, entity, entitySchema, GetKeySchema(entitySchema), GetConcurrencySchema(entitySchema), null, null, "/");
            Commands.Add(node);
        }

        protected UpdateCommandNode<T> Split(T aggregNode, string entity, XElement entitySchema, XElement uniqueKeySchema, XElement concurrencySchema,
            DirectRelationship parentRelationship, Dictionary<string, object> parentPropertyValues, string path)
        {
            UpdateCommandNode<T> executeCommand = CreateUpdateCommandNode(aggregNode, entity);

            executeCommand.EntitySchema = entitySchema;
            executeCommand.UniqueKeySchema = uniqueKeySchema;
            executeCommand.ConcurrencySchema = concurrencySchema;
            executeCommand.ParentPropertyValues = parentPropertyValues;
            executeCommand.ParentRelationship = parentRelationship;
            executeCommand.Path = path;

            executeCommand.PropertyValues = GetPropertyValues(aggregNode, entitySchema);

            //executeCommand.FixedUpdatePropertyValues = new Dictionary<string, object>();

            //
            IEnumerable<KeyValuePair<XElement, T>> propertySchemaChildrens = GetPropertySchemaChildrens(aggregNode, entitySchema);
            foreach (KeyValuePair<XElement, T> childrenPair in propertySchemaChildrens)
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
                    Split(children, childEntitySchema, childRelationship as ManyToManyRelationship, executeCommand.PropertyValues, childPath, nodeChildren.UpdateCommandNodes);
                }
                else
                {
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
            }

            return executeCommand;
        }

        protected void Split(IEnumerable<T> children, XElement childEntitySchema, ManyToManyRelationship manyToManyRelationship, Dictionary<string, object> parentPropertyValues, string childPath,
            ICollection<UpdateCommandNode<T>> childNodes)
        {
            string childEntity = childEntitySchema.Attribute(SchemaVocab.Name).Value;

            XElement mmKeySchema = TransKeySchema(manyToManyRelationship, out XElement mmEntitySchema);
            string mmEntity = mmEntitySchema.Attribute(SchemaVocab.Name).Value;
            XElement mmConcurrencySchema = GetConcurrencySchema(mmEntitySchema);

            int index = 0;
            foreach (T child in children)
            {
                Dictionary<string, object> childPropertyValues = GetPropertyValues(child, childEntitySchema);
                Dictionary<string, object> mmPropertyValues = TransPropertyValues(manyToManyRelationship, parentPropertyValues, childPropertyValues);

                Dictionary<string, object> mmUpdatePropertyValues = new Dictionary<string, object>(mmPropertyValues);

                UpdateCommandNode<T> mmExecuteCommand = CreateUpdateCommandNode(child, childEntity);

                mmExecuteCommand.EntitySchema = mmEntitySchema;
                mmExecuteCommand.UniqueKeySchema = mmKeySchema;
                mmExecuteCommand.ConcurrencySchema = mmConcurrencySchema;
                mmExecuteCommand.PropertyValues = mmPropertyValues;
                mmExecuteCommand.ParentPropertyValues = parentPropertyValues;
                mmExecuteCommand.ParentRelationship = manyToManyRelationship.DirectRelationships[0];
                mmExecuteCommand.FixedUpdatePropertyValues = mmUpdatePropertyValues;
                mmExecuteCommand.Path = string.Format("{0}[{1}]", childPath, index);

                childNodes.Add(mmExecuteCommand);
                index++;
            }
        }

        protected UpdateCommandNode<T> CreateUpdateCommandNode(T aggregNode, string entity)
        {
            // default(T) is null
            return (ExecuteAggregationHelper as IUpdateAggregationHelper<T>).CreateUpdateCommandNode(aggregNode, default(T), entity, Schema, Aggreg, default(T));
        }


    }
}
