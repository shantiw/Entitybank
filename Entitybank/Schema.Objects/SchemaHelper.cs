using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    public static class SchemaHelper
    {
        public static XElement GetEntitySchema(this XElement schema, string entity)
        {
            return schema.Elements(SchemaVocab.Entity).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == entity);
        }

        public static XElement GetEntitySchemaByCollection(this XElement schema, string collection)
        {
            return schema.Elements(SchemaVocab.Entity).FirstOrDefault(x => x.Attribute(SchemaVocab.Collection).Value == collection);
        }

        // overload
        public static XElement GetPropertySchema(this XElement schema, string entity, string property)
        {
            XElement entitySchema = schema.Elements(SchemaVocab.Entity).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == entity);
            if (entitySchema == null) return null;

            return GetPropertySchema(entitySchema, property);
        }

        internal static XElement GetPropertySchema(XElement entitySchema, string property)
        {
            return entitySchema.Elements(SchemaVocab.Property).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == property);
        }

        // overload
        public static XElement GetKeySchema(this XElement schema, string entity)
        {
            XElement entitySchema = GetEntitySchema(schema, entity);
            return GetKeySchema(entitySchema);
        }

        internal static XElement GetKeySchema(XElement entitySchema)
        {
            XElement keySchema = new XElement(entitySchema.Name);
            IEnumerable<XElement> Properties = entitySchema.Elements(SchemaVocab.Property)
                .Where(x => x.Elements(SchemaVocab.Annotation).Any(a => a.Attribute(SchemaVocab.Name).Value == "Key"));
            keySchema.Add(Properties);
            return keySchema;
        }

        // overload
        internal static XElement GetConcurrencySchema(this XElement schema, string entity)
        {
            XElement entitySchema = GetEntitySchema(schema, entity);
            return GetConcurrencySchema(entitySchema);
        }

        internal static XElement GetConcurrencySchema(XElement entitySchema)
        {
            XElement concurrencySchema = new XElement(entitySchema.Name);
            IEnumerable<XElement> Properties = entitySchema.Elements(SchemaVocab.Property)
                .Where(x => x.Elements(SchemaVocab.Annotation).Any(a =>
                    a.Attribute(SchemaVocab.Name).Value == "Timestamp" ||
                    a.Attribute(SchemaVocab.Name).Value == "RowVersion" ||
                    a.Attribute(SchemaVocab.Name).Value == "ConcurrencyCheck"));
            concurrencySchema.Add(Properties);
            return concurrencySchema.HasElements ? concurrencySchema : null;
        }

        internal static XElement GetRelationshipSchema(this XElement schema, string name)
        {
            return schema.Elements(SchemaVocab.Relationship).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == name);
        }

        // Department.College.University.Name
        internal static XElement GenerateExtendPropertySchema(this XElement schema, string entity, string property)
        {
            List<string> relationships = new List<string>();

            XElement entitySchema = schema.GetEntitySchema(entity);
            XElement enttSchema = entitySchema;

            string[] ss = property.Split('.');
            for (int i = 0; i < ss.Length - 1; i++)
            {
                string entt = enttSchema.Attribute(SchemaVocab.Name).Value;
                string prop = ss[i];

                XElement propSchema = enttSchema.Elements(SchemaVocab.Property).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == prop);
                if (propSchema == null) throw new SchemaException(string.Format(ErrorMessages.NotFoundProperty, prop, entt));

                XAttribute entityAttr = propSchema.Attribute(SchemaVocab.Entity);
                if (entityAttr == null) throw new SchemaException(string.Format(ErrorMessages.NonEntityProperty, prop, entt));

                XAttribute propAttr = propSchema.Attribute(SchemaVocab.Property);
                if (propAttr != null) throw new SchemaException(string.Format(ErrorMessages.NonEntityProperty, prop, entt));

                XAttribute relationshipAttr = propSchema.Attribute(SchemaVocab.Relationship);
                if (relationshipAttr == null) throw new SchemaException(string.Format(SchemaMessages.RelationshipRequired, prop, entt));

                relationships.Add(new ManyToOneRelationship(relationshipAttr.Value, entt, entityAttr.Value, schema).ToString());

                XElement navEntitySchema = schema.GetEntitySchema(entityAttr.Value);
                enttSchema = navEntitySchema ?? throw new SchemaException(string.Format(ErrorMessages.NotFoundEntity, entityAttr.Value));
            }
            string lastProp = ss[ss.Length - 1];
            XElement lastPropSchema = enttSchema.Elements(SchemaVocab.Property).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == lastProp);
            if (lastPropSchema == null) throw new SchemaException(string.Format(ErrorMessages.NotFoundProperty, lastProp, enttSchema.Attribute(SchemaVocab.Name).Value));

            XElement extPropertySchema = new XElement(SchemaVocab.Property);
            extPropertySchema.SetAttributeValue(SchemaVocab.Name, property);
            extPropertySchema.SetAttributeValue(SchemaVocab.Entity, enttSchema.Attribute(SchemaVocab.Name).Value);
            extPropertySchema.SetAttributeValue(SchemaVocab.Property, lastProp);
            extPropertySchema.SetAttributeValue(SchemaVocab.Relationship, string.Join(",", relationships));

            entitySchema.Add(extPropertySchema);
            return extPropertySchema;
        }

        // University.Colleges.Departments
        internal static XElement[] GenerateExpandPropertyPath(this XElement schema, XElement parentSchema, string property)
        {
            List<XElement> xPropertyPath = new List<XElement>();

            XElement xParent = parentSchema;
            string[] properties = property.Split('.');

            int breakaway = properties.Length;

            int index = 0;
            while (index < properties.Length)
            {
                XElement xProperty = null;

                string propName = string.Empty;
                for (int i = index; i < properties.Length; i++)
                {
                    propName += properties[i];
                    xProperty = xParent.Elements(SchemaVocab.Property).FirstOrDefault(p => p.Attribute(SchemaVocab.Name).Value == propName);
                    if (xProperty == null) continue;

                    xPropertyPath.Add(xProperty);
                    xParent = xProperty;

                    index = i + 1;
                    break;
                }

                //
                if (xProperty == null)
                {
                    if (xParent.Name.LocalName == SchemaVocab.Entity)
                        throw new SchemaException(string.Format(ErrorMessages.NotFoundProperty, property, xParent.Attribute(SchemaVocab.Name).Value));

                    breakaway = index;
                    break;
                }
            }

            //
            for (int i = breakaway; i < properties.Length; i++)
            {
                XElement xProperty = GenerateExpandPropertySchema(properties[index], xParent, schema);
                xPropertyPath.Add(xProperty);
                xParent = xProperty;
            }

            return xPropertyPath.ToArray();
        }

        private static XElement GenerateExpandPropertySchema(string property, XElement xParent, XElement schema)
        {
            XElement xParentEntity;
            XAttribute collectionAttr = xParent.Attribute(SchemaVocab.Collection);
            if (collectionAttr == null)
            {
                XAttribute entityAttr = xParent.Attribute(SchemaVocab.Entity);
                XAttribute propertyAttr = xParent.Attribute(SchemaVocab.Property);
                if (entityAttr != null && propertyAttr == null)
                {
                    xParentEntity = schema.GetEntitySchema(entityAttr.Value);
                }

                throw new SchemaException(string.Format(ErrorMessages.NonEntityProperty,
                    xParent.Attribute(SchemaVocab.Name).Value, GetNamePath(xParent)));
            }
            else
            {
                xParentEntity = schema.GetEntitySchemaByCollection(collectionAttr.Value);
            }

            XElement xProperty = xParentEntity.Elements(SchemaVocab.Property).FirstOrDefault(p => p.Attribute(SchemaVocab.Name).Value == property);
            if (xProperty == null)
                throw new SchemaException(string.Format(ErrorMessages.NotFoundProperty, property, xParentEntity.Attribute(SchemaVocab.Name).Value));

            xProperty = new XElement(xProperty);
            xProperty.RemoveNodes();
            xParent.Add(xProperty);

            return xProperty;
        }

        internal static string GetNamePath(XElement element)
        {
            List<string> path = new List<string>();
            while (element != null)
            {
                path.Add(element.Attribute(SchemaVocab.Name).Value);
                element = element.Parent;
            }
            path.Reverse();
            return string.Join("/", path);
        }

        internal static TimeSpan GetTimezoneOffset(this XElement schema)
        {
            string timezoneOffset = schema.Attribute(SchemaVocab.TimezoneOffset).Value;
            if (timezoneOffset.StartsWith("+")) timezoneOffset = timezoneOffset.Substring(1);
            return TimeSpan.Parse(timezoneOffset);
        }

        internal static IEnumerable<DirectRelationship> GetDirectRelationships(this XElement schema, string entity)
        {
            Dictionary<string, DirectRelationship> directRelationships = new Dictionary<string, DirectRelationship>();

            XElement entitySchema = schema.GetEntitySchema(entity);

            IEnumerable<XElement> collProps = entitySchema.Elements(SchemaVocab.Property).Where(p => p.Attribute(SchemaVocab.Collection) != null);
            foreach (XElement collProp in collProps)
            {
                string collection = collProp.Attribute(SchemaVocab.Collection).Value;
                string relatedEntity = schema.GetEntitySchemaByCollection(collection).Attribute(SchemaVocab.Name).Value;
                string relationshipString = collProp.Attribute(SchemaVocab.Relationship).Value;
                Relationship oRelationship = new Relationship(relationshipString, entity, relatedEntity, schema);
                DirectRelationship first = oRelationship.DirectRelationships[0];
                string key = first.ToString();
                if (!directRelationships.ContainsKey(key))
                {
                    directRelationships.Add(key, first);
                }
            }

            return directRelationships.Values;
        }

        internal static void SetDefaultValues(Dictionary<string, object> propertyValues, XElement entitySchema)
        {
            foreach (XElement propertySchema in entitySchema.Elements(SchemaVocab.Property).Where(p => p.Attribute(SchemaVocab.Column) != null))
            {
                string propertyName = propertySchema.Attribute(SchemaVocab.Name).Value;
                if (propertyValues.ContainsKey(propertyName) && propertyValues[propertyName] != null) continue;

                XElement annotation = propertySchema.Elements(SchemaVocab.Annotation).FirstOrDefault(a => a.Attribute(SchemaVocab.Name).Value == "DefaultValue");
                if (annotation == null) continue;

                DefaultValueAttribute attribute = annotation.CreateDefaultValueAttribute(propertySchema);
                {
                    if (propertyValues.ContainsKey(propertyName))
                    {
                        propertyValues[propertyName] = attribute.Value;
                    }
                    else
                    {
                        propertyValues.Add(propertyName, attribute.Value);
                    }
                }
            }
        }

        internal static string GetDisplayName(XElement propertySchema)
        {
            string displayName = propertySchema.Name.LocalName;
            if (propertySchema.Element("DisplayName") != null)
            {
                displayName = propertySchema.Element("DisplayName").CreateDisplayNameAttribute().DisplayName;
            }
            if (propertySchema.Element("Display") != null)
            {
                displayName = propertySchema.Element("Display").CreateDisplayAttribute().GetName();
            }
            return displayName;
        }


    }
}
