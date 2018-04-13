using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Modification
{
    public abstract class CreateAggregation<T> : ExecuteAggregation<T>
    {
        public CreateAggregation(T aggreg, string entity, XElement schema) : this(aggreg, entity, schema, null, null, "/")
        {
        }

        internal protected CreateAggregation(T aggreg, string entity, XElement schema,
            DirectRelationship parentRelationship, Dictionary<string, object> parentPropertyValues, string path) : base(aggreg, entity, schema)
        {
            XElement entitySchema = GetEntitySchema(entity);
            Split(aggreg, entity, entitySchema, GetKeySchema(entitySchema), parentRelationship, parentPropertyValues, path);
        }

        protected void Split(T obj, string entity, XElement entitySchema, XElement uniqueKeySchema,
            DirectRelationship parentRelationship, Dictionary<string, object> parentPropertyValues, string path)
        {
            InsertCommand<T> executeCommand = CreateInsertCommand(obj, entity);

            executeCommand.EntitySchema = entitySchema;
            executeCommand.UniqueKeySchema = uniqueKeySchema;
            executeCommand.ParentPropertyValues = parentPropertyValues;
            executeCommand.ParentRelationship = parentRelationship;
            executeCommand.Path = path;

            //executeCommand.PropertyValues = GetPropertyValues(executeCommand, out Dictionary<XElement, T> childrenDict);

            executeCommand.PropertyValues = GetPropertyValues(executeCommand.AggregNode, executeCommand.EntitySchema);

            SetDefaultValues(executeCommand);

            Commands.Add(executeCommand);

            //
            IEnumerable<KeyValuePair<XElement, T>> propertySchemaChildrens = GetPropertySchemaChildrens(executeCommand.AggregNode, executeCommand.EntitySchema);
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

                if (childRelationship is ManyToManyRelationship)
                {
                    Split(children, childRelationship as ManyToManyRelationship, executeCommand.PropertyValues, path);
                    return;
                }
                else
                {
                    XElement childKeySchema = GetKeySchema(childEntitySchema);

                    int index = 0;
                    foreach (T child in children)
                    {
                        Split(child, childEntity, childEntitySchema, childKeySchema,
                            childRelationship.DirectRelationships[0], executeCommand.PropertyValues,
                            string.Format("{0}[{1}]", childPath, index));
                        index++;
                    }
                }
            }
        }

        protected void Split(IEnumerable<T> children, ManyToManyRelationship manyToManyRelationship, Dictionary<string, object> parentPropertyValues, string path)
        {
            XElement mmKeySchema = TransKeySchema(manyToManyRelationship, out XElement mmEntitySchema);
            string mmEntity = mmEntitySchema.Name.LocalName;

            int index = 0;
            foreach (T child in children)
            {
                Dictionary<string, object> childPropertyValues = GetPropertyValues(child, mmEntitySchema);
                Dictionary<string, object> mmPropertyValues = TransPropertyValues(manyToManyRelationship, parentPropertyValues, childPropertyValues);

                T mmChild = child;
                ResetObjectValues(mmChild, mmPropertyValues);

                InsertCommand<T> mmExecuteCommand = CreateInsertCommand(mmChild, mmEntity);

                mmExecuteCommand.EntitySchema = mmEntitySchema;
                mmExecuteCommand.UniqueKeySchema = mmKeySchema;
                mmExecuteCommand.ParentPropertyValues = parentPropertyValues;
                mmExecuteCommand.ParentRelationship = manyToManyRelationship.DirectRelationships[0];
                mmExecuteCommand.Path = path;
                mmExecuteCommand.PropertyValues = mmPropertyValues;

                SetDefaultValues(mmExecuteCommand);

                Commands.Add(mmExecuteCommand);

                index++;
            }
        }

        protected InsertCommand<T> CreateInsertCommand(T aggregNode, string entity)
        {
            return (ExecuteAggregationHelper as ICreateAggregationHelper<T>).CreateInsertCommand(aggregNode, entity, Schema, Aggreg);
        }


    }
}
