using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Modification
{
    public abstract partial class ExecuteAggregation<T>
    {
        protected XElement GetEntitySchema(string entity)
        {
            return Schema.GetEntitySchema(entity);
        }

        protected XElement GetEntitySchemaByCollection(string collection)
        {
            return Schema.GetEntitySchemaByCollection(collection);
        }

        protected XElement GetKeySchema(XElement entitySchema)
        {
            return SchemaHelper.GetKeySchema(entitySchema);
        }

        protected XElement GetConcurrencySchema(XElement entitySchema)
        {
            return SchemaHelper.GetConcurrencySchema(entitySchema);
        }

        protected XElement GetPropertySchema(XElement entitySchema, string propertyName)
        {
            return entitySchema.Elements(SchemaVocab.Property).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == propertyName);
        }

        protected Relationship GetParentChildrenRelationship(string relationship, string entity, string childEntity)
        {
            Relationship oRelationship = new Relationship(relationship, entity, childEntity, Schema);

            if (oRelationship.DirectRelationships.Length == 1)
            {
                return new OneToManyRelationship(entity, childEntity, oRelationship.DirectRelationships);
            }

            if (oRelationship.DirectRelationships.Length == 2)
            {
                string intermediary = oRelationship.DirectRelationships[0].RelatedEntity;

                XElement xRelationship = Schema.Elements(SchemaVocab.Relationship).FirstOrDefault(r =>
                    r.Attribute(SchemaVocab.Type).Value == SchemaVocab.ManyToMany &&
                    r.Attribute(SchemaVocab.Name) != null && r.Attribute(SchemaVocab.Name).Value == relationship &&
                    r.Elements(SchemaVocab.Relationship).Count() == 2);
                if (xRelationship == null)
                {
                    bool firstIsMatch = IsMatch(SchemaVocab.OneToMany, entity, intermediary, oRelationship.DirectRelationships[0].ToString());
                    if (!firstIsMatch)
                    {
                        firstIsMatch = IsMatch(SchemaVocab.ManyToOne, intermediary, entity, oRelationship.DirectRelationships[0].Reverse().ToString());
                    }
                    if (!firstIsMatch) return null;

                    //
                    bool secondIsMatch = IsMatch(SchemaVocab.ManyToOne, intermediary, childEntity, oRelationship.DirectRelationships[1].ToString());
                    if (!secondIsMatch)
                    {
                        secondIsMatch = IsMatch(SchemaVocab.OneToMany, entity, intermediary, oRelationship.DirectRelationships[1].Reverse().ToString());
                    }
                    if (!secondIsMatch) return null;
                }

                OneToManyRelationship oneToManyRelationship = new OneToManyRelationship(entity, intermediary,
                    new List<DirectRelationship> { oRelationship.DirectRelationships[0] });
                ManyToOneRelationship manyToOneRelationship = new ManyToOneRelationship(intermediary, childEntity,
                    new List<DirectRelationship> { oRelationship.DirectRelationships[1] });

                return new ManyToManyRelationship(oneToManyRelationship, manyToOneRelationship);
            }

            return null;
        }

        private bool IsMatch(string type, string entity, string relatedEntity, string directRelationship)
        {
            IEnumerable<XElement> relationshipSchemas = Schema.Elements(SchemaVocab.Relationship).Where(r =>
                r.Attribute(SchemaVocab.Type).Value == type &&
                r.Attribute(SchemaVocab.Entity).Value == entity &&
                r.Attribute(SchemaVocab.RelatedEntity).Value == relatedEntity &&
                r.Elements(SchemaVocab.Property).Count() == 1);

            foreach (XElement relationshipSchema in relationshipSchemas)
            {
                if (Relationship.Create(relationshipSchema).DirectRelationships[0].ToString() == directRelationship)
                {
                    return true;
                }
            }
            return false;
        }

        // Delete
        protected IEnumerable<DirectRelationship> GetDirectRelationships(string entity)
        {
            return Schema.GetDirectRelationships(entity);
        }

        // Insert
        protected void SetDefaultValues(InsertCommand<T> insertCommand)
        {
            SchemaHelper.SetDefaultValues(insertCommand.PropertyValues, insertCommand.EntitySchema);
        }

        // ManyToMany
        protected XElement TransKeySchema(ManyToManyRelationship relationship, out XElement mmEntitySchema)
        {
            DirectRelationship oneToManyRelationship = relationship.DirectRelationships[0];
            DirectRelationship manyToOneRelationship = relationship.DirectRelationships[1];

            string mmEntity = oneToManyRelationship.RelatedEntity;
            mmEntitySchema = GetEntitySchema(mmEntity);

            XElement mmKeySchema = new XElement(mmEntitySchema.Name);
            IEnumerable<string> mmKeyPropertyNames = oneToManyRelationship.RelatedProperties.Union(manyToOneRelationship.Properties);
            foreach (string mmPropertyName in mmKeyPropertyNames)
            {
                mmKeySchema.Add(mmEntitySchema.Elements(SchemaVocab.Property).First(p => p.Attribute(SchemaVocab.Name).Value == mmPropertyName));
            }

            return mmKeySchema;
        }

        // ManyToMany
        protected Dictionary<string, object> TransPropertyValues(ManyToManyRelationship relationship,
            Dictionary<string, object> parent, Dictionary<string, object> child)
        {
            Dictionary<string, object> propertyValues = new Dictionary<string, object>();

            DirectRelationship firstDirectRelationship = relationship.DirectRelationships[0];
            DirectRelationship secondDirectRelationship = relationship.DirectRelationships[1];

            for (int i = 0; i < firstDirectRelationship.Properties.Length; i++)
            {
                string propertyName = firstDirectRelationship.Properties[i];
                string relatedPropertyName = firstDirectRelationship.RelatedProperties[i];
                propertyValues.Add(relatedPropertyName, parent[propertyName]);
            }

            for (int i = 0; i < secondDirectRelationship.RelatedProperties.Length; i++)
            {
                string relatedPropertyName = secondDirectRelationship.RelatedProperties[i];
                string propertyName = secondDirectRelationship.Properties[i];

                propertyValues.Add(propertyName, child[relatedPropertyName]);
            }

            return propertyValues;
        }

        // ManyToMany
        protected bool IsDeleteTransSetNull(ManyToManyRelationship relationship)
        {
            DirectRelationship firstDirectRelationship = relationship.DirectRelationships[0];

            // pure relationship-table
            if (relationship.DirectRelationships.Length == 2)
            {
                DirectRelationship secondDirectRelationship = relationship.DirectRelationships[1];
                string relationshipEntity = firstDirectRelationship.RelatedEntity;

                if (relationshipEntity == secondDirectRelationship.Entity) // assert
                {
                    List<string> relationshipProperties = new List<string>(firstDirectRelationship.RelatedProperties);
                    relationshipProperties.AddRange(secondDirectRelationship.Properties);

                    XElement entitySchema = Schema.GetEntitySchema(relationshipEntity);

                    List<string> PropertyNames = entitySchema.Elements(SchemaVocab.Property)
                        .Where(x => x.Attribute(SchemaVocab.Column) != null)
                        .Select(x => x.Attribute(SchemaVocab.Name).Value).ToList();

                    if (PropertyNames.Count == relationshipProperties.Count)
                    {
                        bool pure = true;
                        relationshipProperties.Sort();
                        PropertyNames.Sort();
                        for (int i = 0; i < PropertyNames.Count; i++)
                        {
                            if (relationshipProperties[i] == PropertyNames[i]) continue;
                            pure = false;
                        }
                        if (pure) return false;
                    }
                }
            }

            //
            string mmEntity = firstDirectRelationship.RelatedEntity;
            XElement mmEntitySchema = GetEntitySchema(mmEntity);

            for (int i = 0; i < firstDirectRelationship.RelatedProperties.Length; i++)
            {
                string relatedPropertyName = firstDirectRelationship.RelatedProperties[i];
                XElement mmPropertySchema = GetPropertySchema(mmEntitySchema, relatedPropertyName);
                if (mmPropertySchema.Attribute(SchemaVocab.AllowDBNull).Value == SchemaVocab.True)
                {
                    return true;
                }
            }

            return false;
        }


    }
}
