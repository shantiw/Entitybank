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

        // T aggreg, string entity, XElement schema
        protected Func<T, string, XElement, CreateAggregation<T>> CreateCreateAggregation;
        protected Func<T, string, XElement, DeleteAggregation<T>> CreateDeleteAggregation;

        protected List<ExecuteCommand<T>> DeleteCommands = new List<ExecuteCommand<T>>();

        public UpdateAggregation(T aggreg, T original, string entity, XElement schema,
            Func<T, string, XElement, CreateAggregation<T>> createCreateAggregation,
            Func<T, string, XElement, DeleteAggregation<T>> createDeleteAggregation) : base(aggreg, entity, schema)
        {
            Original = original;
            CreateCreateAggregation = createCreateAggregation;
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
            Commands.AddRange(DeleteCommands);
        }

        protected void Split(UpdateCommandNode<T> updateCommandNode)
        {
            // <propertyName,<collectionPropertySchema, T is collection>>
            Dictionary<string, KeyValuePair<XElement, T>> propSchemaChildrenDict = GetPropertySchemaChildrenDict(updateCommandNode.AggregNode, updateCommandNode.EntitySchema);
            Dictionary<string, KeyValuePair<XElement, T>> origPropSchemaChildrenDict = GetPropertySchemaChildrenDict(updateCommandNode.OrigNode, updateCommandNode.EntitySchema);

            foreach (var pair in propSchemaChildrenDict)
            {
                string property = pair.Key;
                XElement propertySchema = pair.Value.Key;
                T childrenT = pair.Value.Value;
                if (origPropSchemaChildrenDict.ContainsKey(property))
                {
                    T origChildrenT = origPropSchemaChildrenDict[property].Value;
                    Update(propertySchema, GetChildren(childrenT), GetChildren(origChildrenT), updateCommandNode);

                    origPropSchemaChildrenDict.Remove(property);
                }
                //else
                //{
                //    // create
                //    string childrenPath = updateCommandNode.Path + propertySchema.Attribute(SchemaVocab.Name).Value;
                //    XElement childEntitySchema = GetEntitySchemaByCollection(propertySchema.Attribute(SchemaVocab.Collection).Value);
                //    string childEntity = childEntitySchema.Attribute(SchemaVocab.Name).Value;

                //    //
                //    string relationshipString = propertySchema.Attribute(SchemaVocab.Relationship).Value;
                //    Relationship childRelationship = GetParentChildrenRelationship(relationshipString, updateCommandNode.Entity, childEntity);
                //    if (childRelationship == null) return;

                //    if (childRelationship is ManyToManyRelationship)
                //    {
                //        ManyToManyRelationship manyToManyRelationship = childRelationship as ManyToManyRelationship;
                //        IEnumerable<ExecuteCommand<T>> commands = GetCreateAggregationCommands(GetChildren(childrenT), childEntitySchema, manyToManyRelationship,
                //            updateCommandNode.PropertyValues, childrenPath, updateCommandNode.Entity, updateCommandNode.Schema);
                //        Commands.AddRange(commands);
                //    }
                //    else
                //    {
                //        int index = 0;
                //        foreach (T child in GetChildren(childrenT))
                //        {
                //            Create(child, childEntitySchema, childRelationship.DirectRelationships[0], updateCommandNode.PropertyValues,
                //                string.Format("{0}[{1}]", childrenPath, index), childEntity, updateCommandNode.Schema);
                //            index++;
                //        }
                //    }
                //}
            }

            //// delete
            //foreach (var pair in origPropSchemaChildrenDict)
            //{
            //    XElement propertySchema = pair.Value.Key;
            //    string childrenPath = updateCommandNode.Path + propertySchema.Attribute(SchemaVocab.Name).Value;
            //    XElement childEntitySchema = GetEntitySchemaByCollection(propertySchema.Attribute(SchemaVocab.Collection).Value);
            //    string childEntity = childEntitySchema.Attribute(SchemaVocab.Name).Value;

            //    //
            //    string relationshipString = propertySchema.Attribute(SchemaVocab.Relationship).Value;
            //    Relationship childRelationship = GetParentChildrenRelationship(relationshipString, updateCommandNode.Entity, childEntity);
            //    if (childRelationship == null) return;

            //    if (childRelationship is ManyToManyRelationship)
            //    {
            //        ManyToManyRelationship manyToManyRelationship = childRelationship as ManyToManyRelationship;
            //        IEnumerable<ExecuteCommand<T>> commands = GetDeleteAggregationCommands(GetChildren(pair.Value.Value), childEntitySchema, manyToManyRelationship,
            //            updateCommandNode.PropertyValues, childrenPath, updateCommandNode.Entity, updateCommandNode.Schema);
            //        DeleteCommands.AddRange(commands);
            //    }
            //    else
            //    {
            //        int index = 0;
            //        foreach (T child in GetChildren(pair.Value.Value))
            //        {
            //            Delete(child, childEntitySchema, childRelationship.DirectRelationships[0], updateCommandNode.PropertyValues,
            //                string.Format("{0}[+{1}]", childrenPath, index), childEntity, updateCommandNode.Schema);
            //            index++;
            //        }
            //    }
            //}
        }

        protected void Update(XElement propertySchema, IEnumerable<T> children, IEnumerable<T> origChildren, UpdateCommandNode<T> updateCommandNode)
        {
            string childrenPath = updateCommandNode.Path + propertySchema.Attribute(SchemaVocab.Name).Value;

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

            if (childRelationship is ManyToManyRelationship)
            {
                Split(children, origChildren, childEntitySchema, childRelationship as ManyToManyRelationship, updateCommandNode.PropertyValues,
                    childrenPath, updateCommandNode.Entity, updateCommandNode.Schema, nodeChildren.UpdateCommandNodes);
                return;
            }

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
                origChildCommand.Path = string.Format("{0}[+{1}]", childrenPath, origIndex);
                origChildCommand.PropertyValues = new Dictionary<string, object>(); // avoid throwing NullReferenceException
                origChildCommand.OrigPropertyValues = GetPropertyValues(origChild, childEntitySchema);
                nodeChildren.UpdateCommandNodes.Add(origChildCommand);
                origIndex++;
            }

            //
            int index = 0;
            foreach (T child in children)
            {
                Dictionary<string, object> childPropertyValues = GetPropertyValues(child, childEntitySchema);

                //
                Dictionary<string, object> childKeyPropertyValues = new Dictionary<string, object>();
                foreach (string property in childKeySchema.Elements(SchemaVocab.Property).Select(x => x.Attribute(SchemaVocab.Name).Value))
                {
                    childKeyPropertyValues.Add(property, childPropertyValues[property]);
                }

                //
                UpdateCommandNode<T> origChildCommandNode = Find(nodeChildren.UpdateCommandNodes, childKeyPropertyValues);
                if (origChildCommandNode == null)
                {
                    Create(child, childEntitySchema, childRelationship.DirectRelationships[0], updateCommandNode.PropertyValues,
                        string.Format("{0}[{1}]", childrenPath, index), childEntity, updateCommandNode.Schema);
                }
                else
                {
                    SetUpdateCommandNode(origChildCommandNode, child);
                    origChildCommandNode.Path = string.Format("{0}[{1}]", childrenPath, index);
                    origChildCommandNode.PropertyValues = childPropertyValues;

                    // recursive
                    Split(origChildCommandNode);
                }
                index++;
            }

            // delete
            IEnumerable<UpdateCommandNode<T>> deleteNodes = nodeChildren.UpdateCommandNodes.Where(p => p.AggregNode == null);
            foreach (UpdateCommandNode<T> origChildCommand in deleteNodes)
            {
                Delete(origChildCommand.OrigNode, origChildCommand.EntitySchema, origChildCommand.ParentRelationship, origChildCommand.ParentPropertyValues,
                    origChildCommand.Path, origChildCommand.Entity, origChildCommand.Schema);
            }
        }

        protected void Split(IEnumerable<T> children, IEnumerable<T> origChildren, XElement childEntitySchema, ManyToManyRelationship manyToManyRelationship,
            Dictionary<string, object> parentPropertyValues, string childrenPath, string parentEntity, XElement schema,
            ICollection<UpdateCommandNode<T>> updateCommandNodes)
        {
            string childEntity = childEntitySchema.Attribute(SchemaVocab.Name).Value;

            XElement mmKeySchema = TransKeySchema(manyToManyRelationship, out XElement mmEntitySchema);
            string mmEntity = mmEntitySchema.Attribute(SchemaVocab.Name).Value;
            XElement mmConcurrencySchema = GetConcurrencySchema(mmEntitySchema);

            int origIndex = 0;
            foreach (T origChild in origChildren)
            {
                Dictionary<string, object> childPropertyValues = GetPropertyValues(origChild, childEntitySchema);
                Dictionary<string, object> mmPropertyValues = TransPropertyValues(manyToManyRelationship, parentPropertyValues, childPropertyValues);

                Dictionary<string, object> mmUpdatePropertyValues = new Dictionary<string, object>(mmPropertyValues);

                UpdateCommandNode<T> mmExecuteCommand = CreateUpdateCommandNode(default(T), origChild, childEntity);
                mmExecuteCommand.EntitySchema = mmEntitySchema;
                mmExecuteCommand.UniqueKeySchema = mmKeySchema;
                mmExecuteCommand.ConcurrencySchema = mmConcurrencySchema;
                mmExecuteCommand.PropertyValues = new Dictionary<string, object>(); // avoid throwing NullReferenceException
                mmExecuteCommand.OrigPropertyValues = mmPropertyValues;
                mmExecuteCommand.ParentPropertyValues = parentPropertyValues;
                mmExecuteCommand.ParentRelationship = manyToManyRelationship.DirectRelationships[0];
                mmExecuteCommand.FixedUpdatePropertyValues = mmUpdatePropertyValues;
                mmExecuteCommand.Path = string.Format("{0}[+{1}]", childrenPath, origIndex);

                updateCommandNodes.Add(mmExecuteCommand);
                origIndex++;
            }

            //
            List<string> createNodePaths = new List<string>();
            List<T> createNodes = new List<T>();

            int index = 0;
            foreach (T child in children)
            {
                Dictionary<string, object> childPropertyValues = GetPropertyValues(child, childEntitySchema);
                Dictionary<string, object> mmPropertyValues = TransPropertyValues(manyToManyRelationship, parentPropertyValues, childPropertyValues);

                UpdateCommandNode<T> origChildCommandNode = Find(updateCommandNodes, mmPropertyValues);
                if (origChildCommandNode == null)
                {
                    createNodePaths.Add(string.Format("{0}[{1}]", childrenPath, index));
                    createNodes.Add(child);
                }
                else
                {
                    // update
                    SetUpdateCommandNode(origChildCommandNode, child);
                    origChildCommandNode.Path = string.Format("{0}[{1}]", childrenPath, index);
                    origChildCommandNode.PropertyValues = mmPropertyValues;
                }
                index++;
            }

            // create
            ExecuteCommand<T>[] createCommands = GetCreateAggregationCommands(createNodes, childEntitySchema, manyToManyRelationship, parentPropertyValues, childrenPath, parentEntity, schema).ToArray();

            // reset path
            for (int i = 0; i < createCommands.Length; i++)
            {
                createCommands[i].Path = createNodePaths[i];
            }
            Commands.AddRange(createCommands);

            // delete
            UpdateCommandNode<T>[] deleteNodes = updateCommandNodes.Where(p => p.AggregNode == null).ToArray();
            ExecuteCommand<T>[] deleteCommands = GetDeleteAggregationCommands(deleteNodes.Select(p => p.OrigNode), childEntitySchema, manyToManyRelationship, parentPropertyValues, childrenPath, parentEntity, schema).ToArray();

            // reset path
            for (int i = 0; i < deleteCommands.Length; i++)
            {
                deleteCommands[i].Path = deleteNodes[i].Path;
            }
            DeleteCommands.AddRange(deleteCommands);
        }

        protected void Create(T aggreg, XElement entitySchema, DirectRelationship parentRelationship, Dictionary<string, object> parentPropertyValues, string path, string entity, XElement schema)
        {
            CreateAggregation<T> createAggregation = CreateCreateAggregation(default(T), entity, schema);
            createAggregation.Split(aggreg, entitySchema, parentRelationship, parentPropertyValues, path);
            Commands.AddRange(createAggregation.ExecuteCommands);
        }

        protected void Delete(T aggreg, XElement entitySchema, DirectRelationship parentRelationship, Dictionary<string, object> parentPropertyValues, string path, string entity, XElement schema)
        {
            DeleteAggregation<T> deleteAggregation = CreateDeleteAggregation(default(T), entity, schema);
            deleteAggregation.Split(aggreg, entitySchema, parentRelationship, parentPropertyValues, path);

            // reverse back
            IEnumerable<ExecuteCommand<T>> executeCommands = deleteAggregation.ExecuteCommands.Reverse();
            DeleteCommands.AddRange(executeCommands);
        }

        protected IEnumerable<ExecuteCommand<T>> GetCreateAggregationCommands(IEnumerable<T> children, XElement childEntitySchema, ManyToManyRelationship manyToManyRelationship,
            Dictionary<string, object> parentPropertyValues, string childrenPath, string parentEntity, XElement schema)
        {
            CreateAggregation<T> createAggregation = CreateCreateAggregation(default(T), parentEntity, schema);
            createAggregation.Split(children, childEntitySchema, manyToManyRelationship, parentPropertyValues, childrenPath);
            return createAggregation.ExecuteCommands;
        }

        protected IEnumerable<ExecuteCommand<T>> GetDeleteAggregationCommands(IEnumerable<T> children, XElement childEntitySchema, ManyToManyRelationship manyToManyRelationship,
            Dictionary<string, object> parentPropertyValues, string childrenPath, string parentEntity, XElement schema)
        {
            DeleteAggregation<T> deleteAggregation = CreateDeleteAggregation(default(T), parentEntity, schema);
            deleteAggregation.Split(children, childEntitySchema, manyToManyRelationship, parentPropertyValues, childrenPath);

            // reverse back
            return deleteAggregation.ExecuteCommands.Reverse();
        }

        // <propertyName,<collectionPropertySchema, T is collection>>
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
