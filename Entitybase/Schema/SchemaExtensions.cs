using XData.Data.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    internal static class SchemaOperation
    {
        public const string Rename = "rename";
        public const string Remove = "remove";
        public const string Add = "add";
        public const string Replace = "replace";
        public const string Entities = "entities";
        public const string Properties = "properties";
        public const string Annotations = "annotations";
        public const string Arguments = "arguments";
        public const string Relationships = "relationships";
    }

    public static class SchemaExtensions
    {
        public static void Modify(this XElement schema, XElement config)
        {
            if (config == null) return;

            foreach (XElement operation in config.Elements())
            {
                switch (operation.Name.ToString())
                {
                    case SchemaOperation.Rename:
                        Rename(schema, operation);
                        break;
                    case SchemaOperation.Remove:
                        foreach (XElement oper in SplitRemoveOperation(operation))
                        {
                            Remove(schema, oper);
                        }
                        break;
                    case SchemaOperation.Add:
                        Add(schema, operation);
                        break;
                    case SchemaOperation.Replace:
                        XAttribute attr = operation.Attribute(SchemaVocab.TimezoneOffset);
                        if (attr != null)
                        {
                            schema.SetAttributeValue(SchemaVocab.TimezoneOffset, attr.Value);
                            break;
                        }
                        Replace(schema, operation);
                        break;
                }
            }
        }

        //<remove entities="User,Role" />
        //<remove properties="DisplayName,CreatedAt,UpdatedAt" entity="User" />
        //<remove annotations="Required,MaxLength" property="DisplayName" entity="User" />
        ////<remove arguments="MaximumLength,MinimumLength" annotatio="StringLength" property="UserName" entity="User" />
        //<remove relationships="User_Employee,Role_User" />
        private static IEnumerable<XElement> SplitRemoveOperation(XElement operation)
        {
            XAttribute attr = operation.Attribute(SchemaOperation.Entities);
            if (attr != null)
            {
                return SplitRemoveOperation(operation, attr.Value, SchemaVocab.Entity);
            }
            attr = operation.Attribute(SchemaOperation.Properties);
            if (attr != null)
            {
                return SplitRemoveOperation(operation, attr.Value, SchemaVocab.Property);
            }
            attr = operation.Attribute(SchemaOperation.Annotations);
            if (attr != null)
            {
                return SplitRemoveOperation(operation, attr.Value, SchemaVocab.Annotation);
            }
            //attr = operation.Attribute(SchemaOperation.Arguments);
            //if (attr != null)
            //{
            //    return SplitRemoveOperation(operation, attr.Value, SchemaVocab.Argument);
            //}
            attr = operation.Attribute(SchemaOperation.Relationships);
            if (attr != null)
            {
                return SplitRemoveOperation(operation, attr.Value, SchemaVocab.Relationship);
            }

            return new List<XElement>() { operation };
        }

        private static List<XElement> SplitRemoveOperation(XElement operation, string values, string name)
        {
            List<XElement> list = new List<XElement>();
            string[] ss = values.Split(new char[] { ',' });
            for (int i = 0; i < ss.Length; i++)
            {
                ss[i] = ss[i].Trim();
                XElement item = new XElement(operation);
                item.SetAttributeValue(name, ss[i]);
                list.Add(item);
            }
            return list;
        }

        // rename Relationship(s)
        //<rename relationship="User_Employee" name="UserEmployee" />
        private static void Rename(XElement schema, XElement operation)
        {
            XAttribute relAttr = operation.Attribute(SchemaVocab.Relationship);
            if (relAttr != null)
            {
                string relationshipName = operation.Attribute(SchemaVocab.Relationship).Value;
                string newName = operation.Attribute(SchemaVocab.Name).Value;
                XElement xrelationship = schema.Elements(SchemaVocab.Relationship).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == relationshipName);
                if (xrelationship != null)
                {
                    xrelationship.SetAttributeValue(SchemaVocab.Name, newName);

                    //<property name="..." entity="..." property="..." relationship="{relationshipName}" />
                    //
                    // or
                    //
                    //<entity name="Employee">
                    //
                    // <entity name="Employee" relationship="{relationshipName}" ... />
                    //
                    //</entity>
                    IEnumerable<XElement> xRefs = schema.Descendants().Where(x => x.Attribute(SchemaVocab.Relationship) != null && x.Attribute(SchemaVocab.Relationship).Value == relationshipName);
                    foreach (XElement xRef in xRefs)
                    {
                        xRef.SetAttributeValue(SchemaVocab.Relationship, newName);
                    }
                }
            }
        }

        // remove node(s)
        //<remove entity="User" />
        //<remove property="DisplayName" entity="User" />
        //<remove annotation="Required" property="DisplayName" entity="User" />
        ////<remove argument="AllowEmptyStrings" annotatio="Required" property="DisplayName" entity="User" />
        //<remove relationship="User_Employee" />
        private static void Remove(XElement schema, XElement operation)
        {
            XAttribute attr = operation.Attribute(SchemaVocab.Entity);
            XAttribute relAttr = operation.Attribute(SchemaVocab.Relationship);
            if (attr != null && relAttr == null)
            {
                XElement xEntity = schema.Elements(SchemaVocab.Entity).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == attr.Value);
                if (xEntity == null) return;

                attr = operation.Attribute(SchemaVocab.Property);
                if (attr == null)
                {
                    xEntity.Remove();
                    return;
                }

                XElement xProperty = xEntity.Elements(SchemaVocab.Property).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == attr.Value);
                if (xProperty == null) return;

                attr = operation.Attribute(SchemaVocab.Annotation);
                if (attr == null)
                {
                    xProperty.Remove();
                    return;
                }

                XElement xAnnotation = xProperty.Elements(SchemaVocab.Annotation).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == attr.Value);
                if (xAnnotation == null) return;

                xAnnotation.Remove();
                return;

                //attr = operation.Attribute(SchemaVocab.Argument);
                //if (attr == null)
                //{
                //    xAnnotation.Remove();
                //    return;
                //}

                //XElement xArgument = xAnnotation.Elements(SchemaVocab.Argument).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == attr.Value);
                //if (xArgument == null) return;

                //xArgument.Remove();
                //return;
            }

            if (relAttr != null)
            {
                XElement xRelationship = schema.Elements(SchemaVocab.Relationship).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == relAttr.Value);
                if (xRelationship == null) return;

                xRelationship.Remove();
                return;
            }
        }

        // add entity(ies) & relationship(s)
        //<add entity="User" collection="Users" primaryKey="Id" ep-TableType="Table">
        //...
        //  <property name="Id" dataType="System.Int32" allowDbNull="false" ...>
        //    ...
        //  </property>
        //...
        //</add>
        //
        //<add relationship="Users_Employees" type="ManyToOne" entity="Users" relatedEntity="Employees">
        //  <property name="EmployeeId" relatedProperty="Id"/>
        //</add>
        private static void Add(XElement schema, XElement operation)
        {
            XAttribute attr = operation.Attribute(SchemaVocab.Entity);
            XAttribute relAttr = operation.Attribute(SchemaVocab.Relationship);
            if (attr != null && relAttr == null)
            {
                string entityName = attr.Value;
                XElement xEntity = schema.Elements(SchemaVocab.Entity).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == entityName);
                if (xEntity != null) throw new SchemaException(string.Format(SchemaMessages.EntityAlreadyExists, entityName));

                string text = operation.ToString();
                int index = text.IndexOf(SchemaVocab.Entity);
                text = text.Substring(0, index) + text.Substring(index + SchemaVocab.Entity.Length);
                text = text.Insert(index, SchemaVocab.Name);
                xEntity = XElement.Parse(text);
                xEntity.Name = SchemaVocab.Entity;

                schema.Add(xEntity);
            }

            if (relAttr != null)
            {
                string relationshipName = relAttr.Value;
                XElement xRelationship = schema.Elements(SchemaVocab.Relationship).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == relationshipName);
                if (xRelationship != null) throw new SchemaException(string.Format(SchemaMessages.RelationshipAlreadyExists, relationshipName));

                string text = operation.ToString();
                int index = text.IndexOf(SchemaVocab.Relationship);
                text = text.Substring(0, index) + text.Substring(index + SchemaVocab.Relationship.Length);
                text = text.Insert(index, SchemaVocab.Name);
                xRelationship = XElement.Parse(text);
                xRelationship.Name = SchemaVocab.Relationship;

                schema.Add(xRelationship);
            }
        }

        // add or reset Entity(ies)'s descendant(s) and attribute(s)
        //<replace entity="User" collection="Users" primaryKey="Id" ep-TableType="Table">
        //...
        //  <property name="Id" dataType="System.Int32" allowDbNull="false" ...>
        //    ...
        //  </property>
        //...
        //</replace>
        private static void Replace(XElement schema, XElement operation)
        {
            XAttribute attr = operation.Attribute(SchemaVocab.Entity);
            XAttribute relAttr = operation.Attribute(SchemaVocab.Relationship);
            if (attr != null && relAttr == null)
            {
                string entityName = attr.Value;
                XElement xEntity = schema.Elements(SchemaVocab.Entity).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == entityName);
                if (xEntity == null) throw new SchemaException(string.Format(SchemaMessages.NotFoundEntity, entityName));

                CopyAttributes(operation, xEntity, new string[] { SchemaVocab.Entity });

                foreach (XElement oProperty in operation.Elements(SchemaVocab.Property))
                {
                    string propertyName = oProperty.Attribute(SchemaVocab.Name).Value;
                    XElement xProperty = xEntity.Elements(SchemaVocab.Property).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == propertyName);
                    if (xProperty == null)
                    {
                        xEntity.Add(oProperty);
                    }
                    else
                    {
                        CopyAttributes(oProperty, xProperty);

                        foreach (XElement oAnnotation in oProperty.Elements(SchemaVocab.Annotation))
                        {
                            string annotationName = oAnnotation.Attribute(SchemaVocab.Name).Value;
                            XElement xAnnotation = xProperty.Elements(SchemaVocab.Annotation).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == annotationName);
                            if (xAnnotation != null)
                            {
                                xAnnotation.Remove();
                            }
                            xProperty.Add(oAnnotation);
                        }
                    }
                }
            }
        }

        private static void CopyAttributes(XElement source, XElement destination)
        {
            ElementHelper.CopyAttributes(source, destination, new string[] { SchemaVocab.Name });
        }

        private static void CopyAttributes(XElement source, XElement destination, params string[] exclusion)
        {
            ElementHelper.CopyAttributes(source, destination, exclusion);
        }


    }
}
