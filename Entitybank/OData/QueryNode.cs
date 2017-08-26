using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.OData
{
    public abstract class QueryNode
    {
        public Relationship Relationship { get; private set; }
        public string Property { get; private set; }
        public string Entity { get; private set; }
        public Query Query { get; private set; }

        public QueryNode[] Children { get; internal protected set; } = new QueryNode[0];

        public string Path { get; set; }

        protected QueryNode(Relationship relationship, string property, string entity,
            string select, string filter, string orderby, XElement schema, ParameterCollection parameterCollection)
        {
            Relationship = relationship;
            Property = property;
            Entity = entity;
            Query = new Query(Entity, select, filter, orderby, schema, parameterCollection);
        }

        internal static QueryNode Create(ExpandProperty property, string select, string filter, string orderby, XElement schema, ParameterCollection parameterCollection)
        {
            if (property is EntityProperty)
            {
                EntityProperty entityProperty = property as EntityProperty;
                return new EntityQueryNode(property.Relationship, property.Name, entityProperty.Entity,
                    select, filter, orderby, schema, parameterCollection);
            }
            else if (property is CollectionProperty)
            {
                CollectionProperty collectionProperty = property as CollectionProperty;
                return new CollectionQueryNode(property.Relationship, property.Name, collectionProperty.Collection,
                    select, filter, orderby, schema, parameterCollection);
            }

            throw new NotSupportedException(property.GetType().ToString()); // never
        }

    }

    public class EntityQueryNode : QueryNode
    {
        public EntityQueryNode(Relationship relationship, string property, string entity,
            string select, string filter, string orderby, XElement schema, ParameterCollection parameterCollection)
            : base(relationship, property, entity, select, filter, orderby, schema, parameterCollection)
        {
        }

    }

    public class CollectionQueryNode : QueryNode
    {
        public string Collection { get; private set; }

        public CollectionQueryNode(Relationship relationship, string property, string collection,
            string select, string filter, string orderby, XElement schema, ParameterCollection parameterCollection)
            : base(relationship, property, GetEntity(schema, collection), select, filter, orderby, schema, parameterCollection)
        {
            Collection = collection;
        }

        private static string GetEntity(XElement schema, string collection)
        {
            XElement entitySchema = schema.Elements(SchemaVocab.Entity).First(x => x.Attribute(SchemaVocab.Collection).Value == collection);
            return entitySchema.Attribute(SchemaVocab.Name).Value;
        }

    }

}
