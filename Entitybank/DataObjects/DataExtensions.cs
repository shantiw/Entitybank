using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.DataObjects
{
    internal static class DataExtensions
    {
        internal static Column CreateColumn(this NativeProperty property, string entity, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(entity);
            XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).First(x => x.Attribute(SchemaVocab.Name).Value == property.Name);
            string table = entitySchema.Attribute(SchemaVocab.Table).Value;
            return new Column(table, propertySchema.Attribute(SchemaVocab.Column).Value);
        }

        internal static Column CreateColumn(this ExtendProperty property, XElement schema)
        {
            ExtendProperty extProperty = property as ExtendProperty;
            XElement extEntitySchema = schema.GetEntitySchema(extProperty.Entity);
            XElement extPropertySchema = extEntitySchema.Elements(SchemaVocab.Property).First(x => x.Attribute(SchemaVocab.Name).Value == extProperty.Property);
            string table = extEntitySchema.Attribute(SchemaVocab.Table).Value;
            return new Column(table, extPropertySchema.Attribute(SchemaVocab.Column).Value);
        }

        internal static ForeignKey CreateForeignKey(this DirectRelationship directRelationship, XElement schema)
        {
            DirectRelationship relationship = (directRelationship is OneToManyDirectRelationship) ? directRelationship.Reverse() : directRelationship;
            return CreateUndirectedForeignKey(relationship, schema);
        }

        internal static ForeignKey CreateUndirectedForeignKey(this DirectRelationship relationship, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(relationship.Entity);
            string table = entitySchema.Attribute(SchemaVocab.Table).Value;
            List<string> columns = new List<string>();
            foreach (string property in relationship.Properties)
            {
                XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).First(x => x.Attribute(SchemaVocab.Name).Value == property);
                columns.Add(propertySchema.Attribute(SchemaVocab.Column).Value);
            }

            XElement relatedEntitySchema = schema.GetEntitySchema(relationship.RelatedEntity);
            string relatedTable = relatedEntitySchema.Attribute(SchemaVocab.Table).Value;
            List<string> relatedColumns = new List<string>();
            foreach (string property in relationship.RelatedProperties)
            {
                XElement relatedPropertySchema = relatedEntitySchema.Elements(SchemaVocab.Property).First(x => x.Attribute(SchemaVocab.Name).Value == property);
                relatedColumns.Add(relatedPropertySchema.Attribute(SchemaVocab.Column).Value);
            }

            return new ForeignKey(table, relatedTable, columns, relatedColumns);
        }

        internal static ForeignKey[] CreateUndirectedForeignKeys(this Relationship relationship, XElement schema)
        {
            List<ForeignKey> foreignKeys = new List<ForeignKey>();

            foreach (DirectRelationship directRelationship in relationship.DirectRelationships)
            {
                ForeignKey foreignKey = directRelationship.CreateUndirectedForeignKey(schema);
                foreignKeys.Add(foreignKey);
            }
            return foreignKeys.ToArray();
        }


    }
}
