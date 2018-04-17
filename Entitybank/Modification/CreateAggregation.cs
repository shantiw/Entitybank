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
        public CreateAggregation(T aggreg, string entity, XElement schema) : base(aggreg, entity, schema)
        {
            if (aggreg == null) return; // UpdateAggregation.Original

            XElement entitySchema = GetEntitySchema(entity);
            Split(aggreg, entitySchema, null, null, "/");
        }

        internal protected void Split(T obj, XElement entitySchema, DirectRelationship parentRelationship, Dictionary<string, object> parentPropertyValues, string path)
        {
            string entity = entitySchema.Attribute(SchemaVocab.Name).Value;

            InsertCommand<T> executeCommand = CreateInsertCommand(obj, entity);

            executeCommand.EntitySchema = entitySchema;
            executeCommand.UniqueKeySchema = GetKeySchema(entitySchema);
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
                    Split(children, childRelationship as ManyToManyRelationship, executeCommand.PropertyValues, childrenPath);
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

        internal protected void Split(IEnumerable<T> children, ManyToManyRelationship manyToManyRelationship, Dictionary<string, object> parentPropertyValues, string childrenPath)
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
                mmExecuteCommand.Path = string.Format("{0}[{1}]", childrenPath, index);
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
