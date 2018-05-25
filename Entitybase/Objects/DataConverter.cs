using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using XData.Data.DataObjects;

namespace XData.Data.Objects
{
    public class DataConverter : DataConverter<DataRowNode>
    {
        protected override DataRowNode Convert(DataRow row, string name = null)
        {
            return new DataRowNode(row, name);
        }

        public override IEnumerable<DataRowNode> Convert(ResultNode resultNode)
        {
            SetDefaultViewSort(resultNode);

            string entity = (resultNode as CollectionResultNode).Entity;
            List<DataRowNode> roots = new List<DataRowNode>();
            foreach (DataRow row in resultNode.Table.Rows)
            {
                DataRowNode rowNode = Convert(row, entity);
                Compose(resultNode, rowNode);
                roots.Add(rowNode);
            }

            Select(resultNode);

            return roots;
        }

        private void Compose(ResultNode resultNode, DataRowNode rowNode)
        {
            foreach (ResultNode childResult in resultNode.Children)
            {
                List<object> key = new List<object>();
                foreach (string relatedKey in childResult.RelatedKey.Keys)
                {
                    key.Add(rowNode.Row[relatedKey]);
                }
                DataRowView[] rowViews = childResult.Table.DefaultView.FindRows(key.ToArray());

                if (childResult is EntityResultNode)
                {
                    if (rowViews.Length == 0)
                    {
                        rowNode.ChildDict.Add(childResult.Name, null);
                        return;
                    }

                    // Assert(rowViews.Length == 1)
                    DataRowNode childRow = Convert(rowViews[0].Row);
                    rowNode.ChildDict.Add(childResult.Name, childRow);

                    Compose(childResult, childRow);
                }
                else if (childResult is CollectionResultNode)
                {
                    IEnumerable<DataRow> rows = rowViews.Select(p => p.Row);

                    CollectionResultNode collectionResultNode = childResult as CollectionResultNode;
                    rows = Order(rows, collectionResultNode.OrderBy);

                    List<DataRowNode> children = new List<DataRowNode>();
                    foreach (DataRow row in rows)
                    {
                        children.Add(Convert(row, collectionResultNode.Entity));
                    }
                    rowNode.ChildrenDict.Add(childResult.Name, children);

                    foreach (DataRowNode childRow in children)
                    {
                        Compose(childResult, childRow);
                    }
                }
                else
                {
                    throw new NotSupportedException(childResult.GetType().ToString()); // never
                }
            }
        }


    }
}
