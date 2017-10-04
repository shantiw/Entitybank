using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    public abstract class Property
    {
        public string Name { get; protected set; }

        public Property(string name)
        {
            Name = name;
        }
    }

    public abstract class FieldProperty : Property
    {
        public FieldProperty(string name) : base(name)
        {
        }

        public static FieldProperty Create(string property, string entity, XElement schema)
        {
            XElement entitySchema = schema.GetEntitySchema(entity);
            XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == property);

            XAttribute collectionattr = propertySchema.Attribute(SchemaVocab.Collection);
            if (collectionattr != null) throw new SyntaxErrorException(string.Format(ErrorMessages.NonFieldProperty, property, entity));

            FieldProperty oProperty;

            XAttribute entityAttr = propertySchema.Attribute(SchemaVocab.Entity);
            XAttribute propertyAttr = propertySchema.Attribute(SchemaVocab.Property);
            if (entityAttr == null)
            {
                XAttribute columnAttr = propertySchema.Attribute(SchemaVocab.Column);
                XAttribute dataTypeAttr = propertySchema.Attribute(SchemaVocab.DataType);
                if (columnAttr == null || dataTypeAttr == null) throw new SyntaxErrorException(string.Format(ErrorMessages.NonNativeProperty, property, entity));

                oProperty = new NativeProperty(property);
            }
            else
            {
                if (propertyAttr == null) throw new SyntaxErrorException(string.Format(ErrorMessages.NonExtendProperty, property, entity));

                XAttribute relationshipAttr = propertySchema.Attribute(SchemaVocab.Relationship);
                if (relationshipAttr == null) throw new SchemaException(string.Format(SchemaMessages.RelationshipRequired, property, entity));

                ManyToOneRelationship relationship = new ManyToOneRelationship(relationshipAttr.Value, entity, entityAttr.Value, schema);
                oProperty = new ExtendProperty(property, entityAttr.Value, propertyAttr.Value, relationship);
            }

            return oProperty;
        }

    }

    public class NativeProperty : FieldProperty
    {
        public NativeProperty(string name) : base(name)
        {
        }
    }

    public class ExtendProperty : FieldProperty
    {
        public string Entity { get; protected set; }
        public string Property { get; protected set; }
        public ManyToOneRelationship Relationship { get; protected set; }

        public ExtendProperty(string name, string entity, string property, ManyToOneRelationship relationship) : base(name)
        {
            Entity = entity;
            Property = property;
            Relationship = relationship;
        }

        public static ExtendProperty GenerateExtendProperty(string property, string entity, XElement schema)
        {
            XElement extendPropertySchema = schema.GenerateExtendPropertySchema(entity, property);

            XAttribute entityAttr = extendPropertySchema.Attribute(SchemaVocab.Entity);
            XAttribute propertyAttr = extendPropertySchema.Attribute(SchemaVocab.Property);
            XAttribute relationshipAttr = extendPropertySchema.Attribute(SchemaVocab.Relationship);
            ManyToOneRelationship relationship = new ManyToOneRelationship(relationshipAttr.Value, entity, entityAttr.Value, schema);

            return new ExtendProperty(property, entityAttr.Value, propertyAttr.Value, relationship);
        }

    }

    public class ExpandProperty : Property
    {
        public Relationship Relationship { get; protected set; }

        protected ExpandProperty(string name, Relationship relationship) : base(name)
        {
            Relationship = relationship;
        }

        internal static ExpandProperty Create(string property, XElement[] propertyPath, XElement parentSchema, XElement schema)
        {
            XElement last = propertyPath[propertyPath.Length - 1];

            IEnumerable<string> relationshipStrings = propertyPath.Select(p => p.Attribute(SchemaVocab.Relationship).Value);
            string relationshipString = string.Join(",", relationshipStrings);

            string parentEntity = GetParentEntitySchema(schema, parentSchema).Attribute(SchemaVocab.Name).Value;
            string lastEntity = GetParentEntitySchema(schema, last).Attribute(SchemaVocab.Name).Value;

            Relationship relationship = new Relationship(relationshipString, parentEntity, lastEntity, schema);

            XAttribute collectionAttr = last.Attribute(SchemaVocab.Collection);
            if (collectionAttr == null)
            {
                return new EntityProperty(property, lastEntity, relationship);
            }
            else
            {
                return new CollectionProperty(property, collectionAttr.Value, relationship);
            }
        }

        private static XElement GetParentEntitySchema(XElement schema, XElement parentSchema)
        {
            XElement xEntity;
            XAttribute collectionAttr = parentSchema.Attribute(SchemaVocab.Collection);
            if (collectionAttr == null)
            {
                XAttribute entityAttr = parentSchema.Attribute(SchemaVocab.Entity);
                XAttribute propertyAttr = parentSchema.Attribute(SchemaVocab.Property);
                if (entityAttr != null && propertyAttr == null)
                {
                    xEntity = schema.GetEntitySchema(entityAttr.Value);
                }

                throw new SchemaException(string.Format(ErrorMessages.NonEntityProperty,
                    parentSchema.Attribute(SchemaVocab.Name).Value, SchemaHelper.GetNamePath(parentSchema)));
            }
            else
            {
                xEntity = schema.GetEntitySchemaByCollection(collectionAttr.Value);
            }

            return xEntity;
        }

    }

    public class EntityProperty : ExpandProperty
    {
        public string Entity { get; protected set; }

        public EntityProperty(string name, string entity, Relationship relationship) // ManyToOneRelationship
            : base(name, relationship)
        {
            Entity = entity;
        }
    }

    public class CollectionProperty : ExpandProperty
    {
        public string Collection { get; protected set; }

        public CollectionProperty(string name, string collection, Relationship relationship) : base(name, relationship)
        {
            Collection = collection;
        }
    }

}
