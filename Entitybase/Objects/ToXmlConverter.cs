using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using XData.Data.DataObjects;
using XData.Data.Helpers;

namespace XData.Data.Objects
{
    // application/xml default
    public class ToXmlConverter : DataConverter<XElement>
    {
        protected static readonly XNamespace XSINamespace = TypeHelper.XSINamespace;

        public ToXmlConverter()
        {
            DateFormatter = new DotNETDateFormatter();
        }

        protected override XElement Convert(DataRow row, string name)
        {
            XElement element = new XElement(name);
            foreach (DataColumn column in row.Table.Columns)
            {
                XElement xColumn = new XElement(column.ColumnName);

                object obj = row[column];
                if (obj is DBNull)
                {
                    xColumn.Value = string.Empty;
                    xColumn.SetAttributeValue(XSINamespace + "nil", "true");
                }
                else if (obj is bool)
                {
                    xColumn.Value = ((bool)obj) ? "true" : "false";
                }
                else if (obj is DateTime)
                {
                    xColumn.Value = DateFormatter.Format((DateTime)obj);
                }
                else if (obj is byte[] bytes)
                {
                    xColumn.Value = System.Convert.ToBase64String(bytes);
                }
                else
                {
                    xColumn.Value = obj.ToString();
                }

                element.Add(xColumn);
            }
            return element;
        }

        public override IEnumerable<XElement> Convert(ResultNode resultNode)
        {
            List<XElement> elements = new List<XElement>();

            DataConverter dataConverter = new DataConverter() { DateFormatter = this.DateFormatter };
            IEnumerable<DataRowNode> roots = dataConverter.Convert(resultNode);
            foreach (DataRowNode root in roots)
            {
                XElement element = Convert(root.Row, root.Entity);
                Compose(root, element);
                elements.Add(element);
            }

            return elements;
        }

        private void Compose(DataRowNode node, XElement element)
        {
            foreach (KeyValuePair<string, DataRowNode> childPair in node.ChildDict)
            {
                XElement child = Convert(childPair.Value.Row, childPair.Key);
                element.Add(child);
                Compose(childPair.Value, child);
            }

            foreach (KeyValuePair<string, ICollection<DataRowNode>> childrenPair in node.ChildrenDict)
            {
                XElement children = new XElement(childrenPair.Key);
                foreach (DataRowNode childNode in childrenPair.Value)
                {
                    XElement child = Convert(childNode.Row, childNode.Entity);
                    children.Add(child);
                    Compose(childNode, child);
                }
                element.Add(children);
            }
        }


    }
}
