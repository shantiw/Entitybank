using XData.Data.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    public abstract class Mapper : IMapper
    {
        protected abstract string GetCollectionName(string tableName);

        protected abstract string GetEntityName(string tableName);

        protected virtual string GetPropertyName(string tableName, string columnName)
        {
            return columnName;
        }

        public virtual XElement Map(XElement dbSchema)
        {
            XElement schema = new XElement(dbSchema.Name);
            CopyAttributes(dbSchema, schema);

            List<XElement> xRelationships = new List<XElement>();
            foreach (XElement xTable in dbSchema.Elements(SchemaVocab.Table))
            {
                string tableName = xTable.Attribute(SchemaVocab.Name).Value;
                string entityName = GetEntityName(tableName);
                string collectionName = GetCollectionName(tableName);
                XElement xEntity = new XElement(SchemaVocab.Entity);
                xEntity.SetAttributeValue(SchemaVocab.Name, entityName);
                xEntity.SetAttributeValue(SchemaVocab.Collection, collectionName);
                xEntity.SetAttributeValue(SchemaVocab.Table, tableName);
                CopyAttributes(xTable, xEntity, new string[] { SchemaVocab.Name });

                foreach (XElement xColumn in xTable.Elements(SchemaVocab.Column))
                {
                    string columnName = xColumn.Attribute(SchemaVocab.Name).Value;
                    string PropertyName = GetPropertyName(tableName, columnName);
                    XElement xProperty = new XElement(SchemaVocab.Property);
                    xProperty.SetAttributeValue(SchemaVocab.Name, PropertyName);
                    xProperty.SetAttributeValue(SchemaVocab.Column, columnName);
                    CopyAttributes(xColumn, xProperty, new string[] { SchemaVocab.Name });
                    xEntity.Add(xProperty);
                }
                schema.Add(xEntity);

                foreach (XElement xforeignKey in xTable.Elements(SchemaVocab.ForeignKey))
                {
                    string relatedTableName = xforeignKey.Attribute(SchemaVocab.RelatedTable).Value;
                    XElement xRelationship = new XElement(SchemaVocab.Relationship);
                    xRelationship.SetAttributeValue(SchemaVocab.Type, SchemaVocab.ManyToOne);
                    xRelationship.SetAttributeValue(SchemaVocab.Entity, entityName);
                    xRelationship.SetAttributeValue(SchemaVocab.RelatedEntity, GetEntityName(relatedTableName));
                    foreach (XElement xColumn in xforeignKey.Elements(SchemaVocab.Column))
                    {
                        XElement xProperty = new XElement(SchemaVocab.Property);
                        xProperty.SetAttributeValue(SchemaVocab.Name, GetPropertyName(tableName, xColumn.Attribute(SchemaVocab.Name).Value));
                        xProperty.SetAttributeValue(SchemaVocab.RelatedProperty, GetPropertyName(relatedTableName, xColumn.Attribute(SchemaVocab.RelatedColumn).Value));
                        xRelationship.Add(xProperty);
                    }
                    xRelationships.Add(xRelationship);
                }
            }
            schema.Add(xRelationships);

            return schema;
        }

        protected static void CopyAttributes(XElement source, XElement destination, params string[] exclusion)
        {
            ElementHelper.CopyAttributes(source, destination, exclusion);
        }


    }
}
