using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using XData.Data.DataObjects;
using XData.Data.Helpers;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    public abstract class DataConverter<T>
    {
        protected static readonly XNamespace XSNamespace = TypeHelper.XSNamespace;

        internal protected DateFormatter DateFormatter { get; set; }

        public virtual IEnumerable<T> Convert(DataTable table, string entity)
        {
            List<T> list = new List<T>();
            foreach (DataRow row in table.Rows)
            {
                T obj = Convert(row, entity);
                list.Add(obj);
            }
            return list;
        }

        protected abstract T Convert(DataRow row, string name);

        public XElement GenerateEntityXsd(DataTable table, string entity, XElement schema)
        {
            return GenerateEntityXsd(table, entity, schema, entity);
        }

        public XElement GenerateCollectionXsd(DataTable table, string entity, XElement schema)
        {
            return GenerateCollectionXsd(table, entity, schema, null);
        }

        protected XElement GenerateEntityXsd(DataTable table, string entity, XElement schema, string entityPropertyName)
        {
            XElement xsd = GenerateCollectionXsd(table, entity, schema, null);
            return GetGenerateEntityXsd(xsd, entity, entityPropertyName);
        }

        protected XElement GetGenerateEntityXsd(XElement collectionXsd, string entity, string entityPropertyName)
        {
            XElement xsd = new XElement(collectionXsd);
            xsd.RemoveNodes();

            XElement entityNode = collectionXsd.Descendants(XSNamespace + "element").First(x => x.Attribute("name") != null && x.Attribute("name").Value == entity);
            entityNode.SetAttributeValue("name", entityPropertyName);
            xsd.Add(entityNode);

            return xsd;
        }

        protected XElement GenerateCollectionXsd(DataTable table, string entity, XElement schema, string collectionPropertyName)
        {
            table.TableName = entity;
            XElement entitySchema = schema.GetEntitySchema(entity);
            string propertyName = string.IsNullOrWhiteSpace(collectionPropertyName) ? entitySchema.Attribute(SchemaVocab.Collection).Value : collectionPropertyName;

            StringBuilder output = new StringBuilder();
            XmlWriter xmlWriter = XmlWriter.Create(output);
            table.WriteXmlSchema(xmlWriter);
            XElement xsd = XElement.Parse(output.ToString());
            xsd.SetAttributeValue("xmlns", null); // remove attribute xmlns
            xsd.SetAttributeValue("id", null); // remove attribute id
            xsd.SetAttributeValue("elementFormDefault", "qualified");
            xsd.SetAttributeValue("attributeFormDefault", "unqualified");
            xsd.Elements().First().RemoveAttributes();
            xsd.Elements().First().SetAttributeValue("name", propertyName);

            for (int i = 0; i < table.Columns.Count; i++)
            {
                DataColumn column = table.Columns[i];
                // Debug.Assert(column.AllowDBNull);

                XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).FirstOrDefault(p =>
                    p.Attribute(SchemaVocab.Name).Value == column.ColumnName && p.Attribute(SchemaVocab.Column) != null);

                if (propertySchema == null || propertySchema.Attribute(SchemaVocab.AllowDBNull).Value == "true")
                {
                    XElement xProperty = xsd.Descendants().First(x => x.Attribute("name") != null && x.Attribute("name").Value == column.ColumnName && x.Attribute("type") != null);
                    xProperty.SetAttributeValue("nillable", "true");
                }
            }

            return xsd;
        }

        public abstract IEnumerable<T> Convert(ResultNode resultNode);

        public XElement GenerateEntityXsd(ResultNode resultNode, XElement schema)
        {
            XElement xsd = GenerateXsd(resultNode, null, schema);

            if (resultNode is CollectionResultNode)
            {
                return GetGenerateEntityXsd(xsd, resultNode.Entity, resultNode.Entity);
            }

            // if (resultNode is CollectionResultNode)
            return xsd;
        }

        public XElement GenerateCollectionXsd(ResultNode resultNode, XElement schema)
        {
            XElement xsd = GenerateXsd(resultNode, null, schema);
            return xsd;
        }

        protected XElement GenerateXsd(ResultNode resultNode, XElement parentXsd, XElement schema)
        {
            XElement xsd;
            if (resultNode is CollectionResultNode)
            {
                xsd = GenerateCollectionXsd(resultNode.Table, resultNode.Entity, schema, resultNode.Name);
            }
            else // if (resultNode is EntityResultNode)
            {
                xsd = GenerateEntityXsd(resultNode.Table, resultNode.Entity, schema, resultNode.Name);
            }

            if (parentXsd != null)
            {
                XElement last = parentXsd.Descendants(XSNamespace + "element").Last(x => x.Attribute("name") != null && x.Attribute("type") != null);
                xsd = new XElement(xsd.Elements().First());
                last.AddAfterSelf(xsd);
            }

            foreach (ResultNode childNode in resultNode.Children)
            {
                GenerateXsd(childNode, xsd, schema);
            }

            return xsd;
        }

        protected static void SetDefaultViewSort(ResultNode resultNode)
        {
            // for child.Table.DefaultView.FindRows(...)
            foreach (ResultNode child in resultNode.Children)
            {
                // ASC
                child.Table.DefaultView.Sort = string.Join(",", child.RelatedKey.Values);
                SetDefaultViewSort(child);
            }
        }

        protected static IEnumerable<DataRow> Order(IEnumerable<DataRow> rows, Order[] orderby)
        {
            if (orderby == null || orderby.Length == 0) return rows;

            IOrderedEnumerable<DataRow> ordered;
            Order order = orderby[0];
            if (order is AscendingOrder)
            {
                ordered = rows.OrderBy(r => r[order.Property]);
            }
            else if (order is DescendingOrder)
            {
                ordered = rows.OrderByDescending(r => r[order.Property]);
            }
            else
            {
                throw new NotSupportedException(order.GetType().ToString()); // never
            }
            for (int i = 1; i < orderby.Length; i++)
            {
                if (orderby[i] is AscendingOrder)
                {
                    ordered = ordered.ThenBy(r => r[orderby[i].Property]);
                }
                else if (orderby[i] is DescendingOrder)
                {
                    ordered = ordered.ThenByDescending(r => r[orderby[i].Property]);
                }
                else
                {
                    throw new NotSupportedException(order.GetType().ToString()); // never
                }
            }

            return ordered;
        }

        protected static void Select(ResultNode resultNode)
        {
            Select(resultNode.Table, resultNode.Select);
            foreach (ResultNode child in resultNode.Children)
            {
                Select(child);
            }
        }

        private static void Select(DataTable table, string[] select)
        {
            List<DataColumn> willBeRemoved = new List<DataColumn>();
            foreach (DataColumn column in table.Columns)
            {
                if (select.Contains(column.ColumnName)) continue;
                willBeRemoved.Add(column);
            }

            foreach (DataColumn column in willBeRemoved)
            {
                table.Columns.Remove(column);
            }
        }


    }
}
