using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    // Annotations: Key, Timestamp/RowVersion, DefaultValue, MaxLength, Required, 
    public class SchemaReviser : ISchemaReviser
    {
        public virtual XElement Revise(XElement schema)
        {
            XElement revised = GetRevised(schema);
            DeriveProperties(revised);
            return revised;
        }

        protected XElement GetRevised(XElement schema)
        {
            XElement revised = new XElement(schema);

            List<XElement> xKeyAllEntities = new List<XElement>();
            foreach (XElement xEntity in revised.Elements(SchemaVocab.Entity))
            {
                // Key
                XAttribute attr = xEntity.Attribute(SchemaVocab.PrimaryKey);
                if (attr != null)
                {
                    string[] primaryKey = attr.Value.Split(new char[] { ',' });
                    int propertyCount = xEntity.Elements(SchemaVocab.Property).Count();
                    if (primaryKey.Length == propertyCount) xKeyAllEntities.Add(xEntity);

                    foreach (string propertyName in primaryKey)
                    {
                        XElement xProperty = xEntity.Elements(SchemaVocab.Property).First(x => x.Attribute(SchemaVocab.Column).Value == propertyName);
                        XElement xAnnotation = new XElement(SchemaVocab.Annotation);
                        xAnnotation.SetAttributeValue(SchemaVocab.Name, "Key");
                        xProperty.Add(xAnnotation);
                    }
                }

                // 
                foreach (XElement xProperty in xEntity.Elements(SchemaVocab.Property))
                {
                    // Required
                    XAttribute allowDbNull = xProperty.Attribute(SchemaVocab.AllowDBNull);
                    if (allowDbNull != null)
                    {
                        if (!bool.Parse(allowDbNull.Value))
                        {
                            if (!xProperty.Elements(SchemaVocab.Annotation).Any(x => x.Attribute(SchemaVocab.Name).Value == "Key"))
                            {
                                XElement xAnnotation = new XElement(SchemaVocab.Annotation);
                                xAnnotation.SetAttributeValue(SchemaVocab.Name, "Required");
                                XElement xArgument = new XElement(SchemaVocab.Argument);
                                xArgument.SetAttributeValue(SchemaVocab.Name, "AllowEmptyStrings");
                                xArgument.SetAttributeValue(SchemaVocab.Value, true);
                                xAnnotation.Add(xArgument);
                                xProperty.Add(xAnnotation);
                            }
                        }
                    }

                    // DefaultValue
                    XAttribute xDefaultValue = xProperty.Attribute(SchemaVocab.DefaultValue);
                    if (xDefaultValue != null)
                    {
                        XElement xAnnotation = new XElement(SchemaVocab.Annotation);
                        xAnnotation.SetAttributeValue(SchemaVocab.Name, "DefaultValue");
                        XElement xArgument = new XElement(SchemaVocab.Argument);
                        xArgument.SetAttributeValue(SchemaVocab.Name, "Value");
                        xArgument.SetAttributeValue(SchemaVocab.Value, xDefaultValue.Value);
                        xAnnotation.Add(xArgument);
                        xProperty.Add(xAnnotation);
                    }

                    // MaxLength
                    XAttribute xMaxLength = xProperty.Attribute(SchemaVocab.MaxLength);
                    if (xMaxLength != null)
                    {
                        XElement xAnnotation = new XElement(SchemaVocab.Annotation);
                        xAnnotation.SetAttributeValue(SchemaVocab.Name, "MaxLength");
                        XElement xArgument = new XElement(SchemaVocab.Argument);
                        xArgument.SetAttributeValue(SchemaVocab.Name, "Length");
                        xArgument.SetAttributeValue(SchemaVocab.Value, xMaxLength.Value);
                        xAnnotation.Add(xArgument);
                        xProperty.Add(xAnnotation);
                    }

                    // Timestamp/RowVersion // SQL Server
                    XAttribute xDataType = xProperty.Attribute(SchemaVocab.DataType);
                    XAttribute xReadonly = xProperty.Attribute(SchemaVocab.Readonly);
                    XAttribute xSqlDbType = xProperty.Attribute("ep-SqlDbType");
                    if (xDataType != null && xReadonly != null && xSqlDbType != null)
                    {
                        if (Type.GetType(xDataType.Value) == typeof(byte[]) && bool.Parse(xReadonly.Value) &&
                            (xSqlDbType.Value == "timestamp") || (xSqlDbType.Value == "rowversion"))
                        {
                            XElement xAnnotation = new XElement(SchemaVocab.Annotation);
                            xAnnotation.SetAttributeValue(SchemaVocab.Name, "RowVersion");
                            xProperty.Add(xAnnotation);
                        }
                    }
                }
            }

            // relationship // many-to-many
            foreach (XElement xEntity in xKeyAllEntities)
            {
                string entityName = xEntity.Attribute(SchemaVocab.Name).Value;
                XElement[] xRelationships = revised.Elements(SchemaVocab.Relationship).Where(x => x.Attribute(SchemaVocab.Entity).Value == entityName).ToArray();
                if (xRelationships.Length == 2)
                {
                    XElement xRelationship = new XElement(SchemaVocab.Relationship);
                    xRelationship.SetAttributeValue(SchemaVocab.Type, SchemaVocab.ManyToMany);
                    xRelationship.SetAttributeValue(SchemaVocab.Entity, xRelationships[0].Attribute(SchemaVocab.RelatedEntity).Value);
                    xRelationship.SetAttributeValue(SchemaVocab.RelatedEntity, xRelationships[1].Attribute(SchemaVocab.RelatedEntity).Value);
                    XElement first = new XElement(SchemaVocab.Relationship);
                    first.SetAttributeValue(SchemaVocab.Type, SchemaVocab.OneToMany);
                    first.SetAttributeValue(SchemaVocab.Entity, xRelationships[0].Attribute(SchemaVocab.RelatedEntity).Value);
                    first.SetAttributeValue(SchemaVocab.RelatedEntity, xRelationships[0].Attribute(SchemaVocab.Entity).Value);
                    foreach (XElement xProperty in xRelationships[0].Elements(SchemaVocab.Property))
                    {
                        XElement xProp = new XElement(SchemaVocab.Property);
                        xProp.SetAttributeValue(SchemaVocab.Name, xProperty.Attribute(SchemaVocab.RelatedProperty).Value);
                        xProp.SetAttributeValue(SchemaVocab.RelatedProperty, xProperty.Attribute(SchemaVocab.Name).Value);
                        first.Add(xProp);
                    }

                    xRelationship.Add(first);
                    xRelationship.Add(new XElement(xRelationships[1]));
                    revised.Add(xRelationship);
                }
            }

            // set name for relationship
            Dictionary<string, int> names = new Dictionary<string, int>();
            foreach (XElement xRelationship in revised.Elements(SchemaVocab.Relationship))
            {
                string entity = xRelationship.Attribute(SchemaVocab.Entity).Value;
                string relatedEntity = xRelationship.Attribute(SchemaVocab.RelatedEntity).Value;
                string name = string.Format("{0}-{1}", entity, relatedEntity);
                if (names.ContainsKey(name))
                {
                    names[name] = names[name] + 1;
                }
                else
                {
                    names.Add(name, 1);
                }

                string type = xRelationship.Attribute(SchemaVocab.Type).Value;
                xRelationship.RemoveAttributes();
                xRelationship.SetAttributeValue(SchemaVocab.Name, name + "#" + names[name]);
                xRelationship.SetAttributeValue(SchemaVocab.Type, type);
                xRelationship.SetAttributeValue(SchemaVocab.Entity, entity);
                xRelationship.SetAttributeValue(SchemaVocab.RelatedEntity, relatedEntity);
            }
            foreach (KeyValuePair<string, int> pair in names)
            {
                string name = pair.Key;
                if (pair.Value == 1)
                {
                    XElement xRelationship = revised.Elements(SchemaVocab.Relationship).First(x => x.Attribute(SchemaVocab.Name).Value == name + "#1");
                    xRelationship.SetAttributeValue(SchemaVocab.Name, name);
                }
            }

            return revised;
        }

        // entity property & collection property
        private void DeriveProperties(XElement schema)
        {
            List<XElement> xRelationships = new List<XElement>(schema.Elements(SchemaVocab.Relationship));

            IEnumerable<XElement> xManyToManyRelationships = schema.Elements(SchemaVocab.Relationship).Where(x => x.Attribute(SchemaVocab.Type).Value == SchemaVocab.ManyToMany);
            foreach (XElement relationship in xManyToManyRelationships)
            {
                List<string> mmProperties = new List<string>();
                XElement xOneToMany = relationship.Elements(SchemaVocab.Relationship).First(x => x.Attribute(SchemaVocab.Type).Value == SchemaVocab.OneToMany);
                string mmEntity = xOneToMany.Attribute(SchemaVocab.RelatedEntity).Value;
                mmProperties.AddRange(xOneToMany.Elements(SchemaVocab.Property).Select(x => x.Attribute(SchemaVocab.RelatedProperty).Value));

                XElement xManyToOne = relationship.Elements(SchemaVocab.Relationship).First(x => x.Attribute(SchemaVocab.Type).Value == SchemaVocab.ManyToOne);
                mmProperties.AddRange(xManyToOne.Elements(SchemaVocab.Property).Select(x => x.Attribute(SchemaVocab.Name).Value));

                XElement xmmEntity = schema.Elements(SchemaVocab.Entity).First(x => x.Attribute(SchemaVocab.Name).Value == mmEntity);
                IEnumerable<string> xmmEntityProperties = xmmEntity.Elements(SchemaVocab.Property).Select(x => x.Attribute(SchemaVocab.Name).Value);

                int count = mmProperties.Count;
                if (mmProperties.Union(xmmEntityProperties).Count() == count)
                {
                    XElement[] moRelationships = xRelationships.Where(x =>
                        x.Attribute(SchemaVocab.Entity).Value == mmEntity &&
                        x.Attribute(SchemaVocab.Type).Value == SchemaVocab.ManyToOne).ToArray();

                    xRelationships.Remove(moRelationships[0]);
                    xRelationships.Remove(moRelationships[1]);
                }
            }

            foreach (XElement xEntity in schema.Elements(SchemaVocab.Entity))
            {
                DeriveProperties(xEntity, xRelationships, schema);
            }
        }

        private void DeriveProperties(XElement xEntity, IEnumerable<XElement> xRelationships, XElement schema)
        {
            string entity = xEntity.Attribute(SchemaVocab.Name).Value;

            Dictionary<string, int> names = new Dictionary<string, int>();

            //
            IEnumerable<XElement> relationships = xRelationships.Where(x => x.Attribute(SchemaVocab.Entity).Value == entity);
            foreach (XElement relationship in relationships)
            {
                bool isManyToMany = relationship.Attribute(SchemaVocab.Type).Value == SchemaVocab.ManyToMany;

                string name = relationship.Attribute(SchemaVocab.RelatedEntity).Value;
                if (isManyToMany)
                {
                    name = schema.Elements(SchemaVocab.Entity).First(x => x.Attribute(SchemaVocab.Name).Value == name).Attribute(SchemaVocab.Collection).Value;
                }
                if (names.ContainsKey(name))
                {
                    names[name] = names[name] + 1;
                }
                else
                {
                    int index = 1;
                    while (xEntity.Elements(SchemaVocab.Property).Any(x => x.Attribute(SchemaVocab.Name).Value == name + "#" + index))
                    {
                        index++;
                    }
                    names.Add(name, index);
                }
                XElement xProperty = new XElement(SchemaVocab.Property);
                xProperty.SetAttributeValue(SchemaVocab.Name, name + "#" + names[name]);
                if (isManyToMany)
                {
                    xProperty.SetAttributeValue(SchemaVocab.Collection, name);
                }
                else
                {
                    xProperty.SetAttributeValue(SchemaVocab.Entity, name);
                }
                xProperty.SetAttributeValue(SchemaVocab.Relationship, relationship.Attribute(SchemaVocab.Name).Value);
                xEntity.Add(xProperty);
            }

            //
            relationships = xRelationships.Where(x => x.Attribute(SchemaVocab.RelatedEntity).Value == entity);
            foreach (XElement relationship in relationships)
            {
                string name = relationship.Attribute(SchemaVocab.Entity).Value;
                name = schema.Elements(SchemaVocab.Entity).First(x => x.Attribute(SchemaVocab.Name).Value == name).Attribute(SchemaVocab.Collection).Value;
                if (names.ContainsKey(name))
                {
                    names[name] = names[name] + 1;
                }
                else
                {
                    int index = 1;
                    while (xEntity.Elements(SchemaVocab.Property).Any(x => x.Attribute(SchemaVocab.Name).Value == name + "#" + index))
                    {
                        index++;
                    }
                    names.Add(name, index);
                }
                XElement xProperty = new XElement(SchemaVocab.Property);
                xProperty.SetAttributeValue(SchemaVocab.Name, name + "#" + names[name]);
                xProperty.SetAttributeValue(SchemaVocab.Collection, name);
                xProperty.SetAttributeValue(SchemaVocab.Relationship, relationship.Attribute(SchemaVocab.Name).Value);
                xEntity.Add(xProperty);
            }

            //
            foreach (KeyValuePair<string, int> pair in names)
            {
                string name = pair.Key;
                if (pair.Value == 1)
                {
                    if (xEntity.Elements(SchemaVocab.Property).Any(x => x.Attribute(SchemaVocab.Name).Value == name)) continue;
                    XElement xProperty = xEntity.Elements(SchemaVocab.Property).First(x => x.Attribute(SchemaVocab.Name).Value == name + "#1");
                    xProperty.SetAttributeValue(SchemaVocab.Name, name);
                }
            }
        }


    }
}
