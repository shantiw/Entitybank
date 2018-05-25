using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Xml.Linq;
using XData.Data.Helpers;
using XData.Data.DataObjects;

namespace XData.Data.Objects
{
    // application/json default
    public class ToJsonConverter : DataConverter<string>
    {
        public ToJsonConverter()
        {
            DateFormatter = new JsonNETFormatter();
        }

        protected override string Convert(DataRow row, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            Append(sb, row);
            sb.Remove(sb.Length - 1, 1);
            sb.Append("}");
            return sb.ToString();
        }

        protected void Append(StringBuilder sb, DataRow row)
        {
            foreach (DataColumn column in row.Table.Columns)
            {
                sb.Append("\"");
                sb.Append(column.ColumnName);
                sb.Append("\":");

                object obj = row[column];
                if (obj is DBNull)
                {
                    sb.Append("null,");
                }
                else if (obj is bool)
                {
                    if ((bool)obj)
                    {
                        sb.Append("true,");
                    }
                    else
                    {
                        sb.Append("false,");
                    }
                }
                else if (obj is DateTime date)
                {
                    sb.Append("\"");
                    sb.Append(DateFormatter.Format(date));
                    sb.Append("\",");
                }
                else if (obj is string value)
                {
                    value = value.Replace("\t", string.Empty).Replace("\v", string.Empty);
                    value = value.Replace("\r\n", "\\r\\n");
                    sb.Append("\"");
                    sb.Append(value);
                    sb.Append("\",");
                }
                else if (IsNumeric(obj))
                {
                    sb.Append(obj.ToString());
                    sb.Append(",");
                }
                else if (obj is byte[] bytes)
                {
                    sb.Append("\"");
                    sb.Append(System.Convert.ToBase64String(bytes));
                    sb.Append("\",");
                }
                else
                {
                    sb.Append("\"");
                    sb.Append(obj.ToString());
                    sb.Append("\",");
                }
            }
        }

        public override IEnumerable<string> Convert(ResultNode resultNode)
        {
            List<string> jsons = new List<string>();

            DataConverter dataConverter = new DataConverter() { DateFormatter = this.DateFormatter };
            IEnumerable<DataRowNode> roots = dataConverter.Convert(resultNode);
            foreach (DataRowNode root in roots)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                Append(sb, root.Row);
                Compose(root, sb);
                sb.Remove(sb.Length - 1, 1);
                sb.Append("}");
                jsons.Add(sb.ToString());
            }
            return jsons;
        }

        private void Compose(DataRowNode node, StringBuilder sb)
        {
            foreach (KeyValuePair<string, DataRowNode> childPair in node.ChildDict)
            {
                sb.Append("\"");
                sb.Append(childPair.Key);
                sb.Append("\":");

                if (childPair.Value == null)
                {
                    sb.Append("null");
                }
                else
                {
                    sb.Append("{");
                    Append(sb, childPair.Value.Row);
                    if (childPair.Value.ChildDict.Count > 0 || childPair.Value.ChildrenDict.Count > 0)
                    {
                        Compose(childPair.Value, sb);
                    }
                    sb.Remove(sb.Length - 1, 1);
                    sb.Append("}");
                }
                sb.Append(",");
            }

            foreach (KeyValuePair<string, ICollection<DataRowNode>> childrenPair in node.ChildrenDict)
            {
                sb.Append("\"");
                sb.Append(childrenPair.Key);
                sb.Append("\":[");
                if (childrenPair.Value.Count == 0)
                {

                }
                else
                {
                    foreach (DataRowNode childNode in childrenPair.Value)
                    {
                        sb.Append("{");
                        Append(sb, childNode.Row);
                        if (childNode.ChildDict.Count > 0 || childNode.ChildrenDict.Count > 0)
                        {
                            Compose(childNode, sb);
                        }
                        sb.Remove(sb.Length - 1, 1);
                        sb.Append("}");
                        sb.Append(",");
                    }
                    sb.Remove(sb.Length - 1, 1);
                }
                sb.Append("]");
                sb.Append(",");
            }
        }

        protected static bool IsNumeric(object obj)
        {
            return TypeHelper.IsNumeric(obj.GetType());
        }


    }
}
