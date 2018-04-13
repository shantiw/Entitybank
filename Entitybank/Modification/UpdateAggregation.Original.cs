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
        public T Original { get; private set; }

        protected Func<T, string, XElement, DirectRelationship, Dictionary<string, object>, string, CreateAggregation<T>> CreateCreateAggregation;
        protected Func<T, string, XElement, string, DeleteAggregation<T>> CreateDeleteAggregation;

        protected List<ExecuteCommand<T>> DeleteCommands = new List<ExecuteCommand<T>>();

        public UpdateAggregation(T aggreg, T original, string entity, XElement schema,
            Func<T, string, XElement, DirectRelationship, Dictionary<string, object>, string, CreateAggregation<T>> CreateCreateAggregation,
            Func<T, string, XElement, string, DeleteAggregation<T>> createDeleteAggregation) : base(aggreg, entity, schema)
        {
            Original = original;
            CreateDeleteAggregation = createDeleteAggregation;
            CreateDeleteAggregation = createDeleteAggregation;

            XElement entitySchema = GetEntitySchema(entity);

            UpdateCommandNode<T> updateCommandNode = CreateUpdateCommandNode(aggreg, original, entity);
            updateCommandNode.EntitySchema = entitySchema;
            updateCommandNode.UniqueKeySchema = GetKeySchema(entitySchema);
            updateCommandNode.ConcurrencySchema = GetConcurrencySchema(entitySchema);
            updateCommandNode.ParentPropertyValues = null;
            updateCommandNode.ParentRelationship = null;
            updateCommandNode.Path = "/";
            updateCommandNode.PropertyValues = GetPropertyValues(aggreg, entitySchema);
            updateCommandNode.OrigPropertyValues = GetPropertyValues(original, entitySchema);

            Commands.Add(updateCommandNode);

            Split(updateCommandNode);

            DeleteCommands.Reverse();
            foreach (DeleteCommand<T> deleteCommand in DeleteCommands)
            {
                Commands.Add(deleteCommand);
            }
        }

        protected void Split(UpdateCommandNode<T> updateCommandNode)
        {
            // <propertyName,<collectionPropertySchema, T is collection>>
            Dictionary<string, KeyValuePair<XElement, T>> propSchemaChildrenDict = GetPropertySchemaChildrenDict(updateCommandNode.AggregNode, updateCommandNode.EntitySchema);
            Dictionary<string, KeyValuePair<XElement, T>> origPropSchemaChildrenDict = GetPropertySchemaChildrenDict(updateCommandNode.OrigNode, updateCommandNode.EntitySchema);

            foreach (var pair in propSchemaChildrenDict)
            {
                string property = pair.Key;
                if (origPropSchemaChildrenDict.ContainsKey(property))
                {
                    Update(pair.Value.Key, GetChildren(pair.Value.Value), GetChildren(origPropSchemaChildrenDict[property].Value), updateCommandNode);

                    origPropSchemaChildrenDict.Remove(property);
                }
                else
                {
                    XElement propertySchema = pair.Value.Key;
                    string childPath = updateCommandNode.Path + propertySchema.Attribute(SchemaVocab.Name).Value;
                    XElement childEntitySchema = GetEntitySchemaByCollection(propertySchema.Attribute(SchemaVocab.Collection).Value);
                    string childEntity = childEntitySchema.Attribute(SchemaVocab.Name).Value;

                    //
                    string relationshipString = propertySchema.Attribute(SchemaVocab.Relationship).Value;
                    Relationship childRelationship = GetParentChildrenRelationship(relationshipString, updateCommandNode.Entity, childEntity);
                    if (childRelationship == null) return;

                    int index = 0;
                    foreach (T child in GetChildren(pair.Value.Value))
                    {
                        Create(child, childEntity, updateCommandNode.Schema,
                            childRelationship.DirectRelationships[0], updateCommandNode.PropertyValues,
                            string.Format("{0}[{1}]", childPath, index));
                        index++;
                    }
                }
            }

            //
            foreach (var pair in origPropSchemaChildrenDict)
            {
                XElement propertySchema = pair.Value.Key;
                string childPath = updateCommandNode.Path + propertySchema.Attribute(SchemaVocab.Name).Value;
                XElement childEntitySchema = GetEntitySchemaByCollection(propertySchema.Attribute(SchemaVocab.Collection).Value);
                string childEntity = childEntitySchema.Attribute(SchemaVocab.Name).Value;

                int index = 0;
                foreach (T child in GetChildren(pair.Value.Value))
                {
                    Delete(child, childEntity, updateCommandNode.Schema, string.Format("{0}[+{1}]", childPath, index));
                    index++;
                }
            }
        }

        protected void Update(XElement propertySchema, IEnumerable<T> children, IEnumerable<T> origChildren, UpdateCommandNode<T> updateCommandNode)
        {
            string childPath = updateCommandNode.Path + propertySchema.Attribute(SchemaVocab.Name).Value;

            //
            XElement childEntitySchema = GetEntitySchemaByCollection(propertySchema.Attribute(SchemaVocab.Collection).Value);
            string childEntity = childEntitySchema.Attribute(SchemaVocab.Name).Value;

            //
            string relationshipString = propertySchema.Attribute(SchemaVocab.Relationship).Value;
            Relationship childRelationship = GetParentChildrenRelationship(relationshipString, updateCommandNode.Entity, childEntity);
            if (childRelationship == null) return;

            //
            UpdateCommandNodeChildren<T> nodeChildren = new UpdateCommandNodeChildren<T>(childRelationship.DirectRelationships[0], updateCommandNode.Path);
            updateCommandNode.ChildrenCollection.Add(nodeChildren);

            //if (childRelationship is ManyToManyRelationship)
            //{
            //    Split(children, childRelationship as ManyToManyRelationship, executeCommand.PropertyValues, executeCommand.Path, nodeChildren.UpdateCommandNodes);
            //}

            //
            XElement childKeySchema = GetKeySchema(childEntitySchema);
            XElement childConcurrencySchema = GetConcurrencySchema(childEntitySchema);

            //          
            int origIndex = 0;
            foreach (T origChild in origChildren)
            {
                UpdateCommandNode<T> origChildCommand = CreateUpdateCommandNode(default(T), origChild, childEntity);
                origChildCommand.EntitySchema = childEntitySchema;
                origChildCommand.UniqueKeySchema = childKeySchema;
                origChildCommand.ConcurrencySchema = childConcurrencySchema;
                origChildCommand.ParentPropertyValues = updateCommandNode.PropertyValues;
                origChildCommand.ParentRelationship = childRelationship.DirectRelationships[0];
                origChildCommand.Path = string.Format("{0}[+{1}]", childPath, origIndex);
                origChildCommand.PropertyValues = null;
                origChildCommand.OrigPropertyValues = GetPropertyValues(origChild, childEntitySchema);
                nodeChildren.UpdateCommandNodes.Add(origChildCommand);
                origIndex++;
            }

            int index = 0;
            foreach (T child in children)
            {
                Dictionary<string, object> childPropertyValues = GetPropertyValues(child, childEntitySchema);

                Dictionary<string, object> childKeyPropertyValues = new Dictionary<string, object>();
                foreach (string property in childKeySchema.Elements(SchemaVocab.Property).Select(x => x.Attribute(SchemaVocab.Name).Value))
                {
                    childKeyPropertyValues.Add(property, childPropertyValues[property]);
                }
                UpdateCommandNode<T> origChildCommandNode = Find(nodeChildren.UpdateCommandNodes, childKeyPropertyValues);

                if (origChildCommandNode == null)
                {
                    Create(child, childEntity, updateCommandNode.Schema,
                        childRelationship.DirectRelationships[0], updateCommandNode.PropertyValues,
                        string.Format("{0}[{1}]", childPath, index));
                }
                else
                {
                    SetUpdateCommandNode(origChildCommandNode, child);
                    origChildCommandNode.Path = string.Format("{0}[{1}]", childPath, index);
                    origChildCommandNode.PropertyValues = childPropertyValues;

                    Split(origChildCommandNode);
                }
                index++;
            }

            //
            IEnumerable<UpdateCommandNode<T>> nodes = nodeChildren.UpdateCommandNodes.Where(p => p.AggregNode == null);
            foreach (UpdateCommandNode<T> origChildCommand in nodes)
            {
                Delete(origChildCommand.OrigNode, origChildCommand.Entity, origChildCommand.Schema, origChildCommand.Path);
            }
        }

        protected void Create(T aggreg, string entity, XElement schema, DirectRelationship parentRelationship, Dictionary<string, object> parentPropertyValues, string path)
        {
            CreateAggregation<T> createAggregation = CreateCreateAggregation(aggreg, entity, schema, parentRelationship, parentPropertyValues, path);
            foreach (var executeCommand in createAggregation.ExecuteCommands)
            {
                Commands.Add(executeCommand);
            }
        }

        protected void Delete(T aggreg, string entity, XElement schema, string path)
        {
            DeleteAggregation<T> deleteAggregation = CreateDeleteAggregation(aggreg, entity, schema, path);

            // reverse back
            IEnumerable<ExecuteCommand<T>> executeCommands = deleteAggregation.ExecuteCommands.Reverse();

            DeleteCommands.AddRange(executeCommands);
        }

        // T is collection
        protected Dictionary<string, KeyValuePair<XElement, T>> GetPropertySchemaChildrenDict(T aggregNode, XElement entitySchema)
        {
            IEnumerable<KeyValuePair<XElement, T>> propSchemaChildrens = GetPropertySchemaChildrens(aggregNode, entitySchema);

            Dictionary<string, KeyValuePair<XElement, T>> dict = new Dictionary<string, KeyValuePair<XElement, T>>();
            foreach (KeyValuePair<XElement, T> propSchemaChildren in propSchemaChildrens)
            {
                XElement propSchema = propSchemaChildren.Key;
                string property = propSchema.Attribute(SchemaVocab.Name).Value;
                dict.Add(property, propSchemaChildren);
            }

            return dict;
        }

        protected UpdateCommandNode<T> CreateUpdateCommandNode(T aggregNode, T origNode, string entity)
        {
            return (ExecuteAggregationHelper as IUpdateAggregationHelper<T>).CreateUpdateCommandNode(aggregNode, origNode, entity, Schema, Aggreg, Original);
        }

        protected static void SetUpdateCommandNode(UpdateCommandNode<T> childCommand, T aggregNode)
        {
            childCommand.SetAggregNode(aggregNode);
        }

        protected static UpdateCommandNode<T> Find(IEnumerable<UpdateCommandNode<T>> collection, Dictionary<string, object> keyPropertyValues)
        {
            IEnumerable<UpdateCommandNode<T>> result = collection;

            foreach (KeyValuePair<string, object> pair in keyPropertyValues)
            {
                result = collection.Where(p => pair.Value != null && p.OrigPropertyValues[pair.Key].ToString() == pair.Value.ToString());
            }
            return result.FirstOrDefault();
        }


    }
}
