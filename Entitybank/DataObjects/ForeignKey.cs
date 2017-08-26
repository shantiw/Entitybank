using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.DataObjects
{
    public sealed class ForeignKey
    {
        public string Table { get; private set; }
        public string RelatedTable { get; private set; }
        public string[] Columns { get; private set; }
        public string[] RelatedColumns { get; private set; }

        public string TableAlias { get; internal set; }
        public string RelatedTableAlias { get; internal set; }

        public ForeignKey(string table, string relatedTable, IEnumerable<string> columns, IEnumerable<string> relatedColumns)
        {
            Table = table;
            RelatedTable = relatedTable;
            Columns = columns.ToArray();
            RelatedColumns = relatedColumns.ToArray();
        }

        public ForeignKey(string foreignKey)
        {
            string Split(string value, out string[] columns)
            {
                int left = value.IndexOf('(');
                string table = value.Substring(0, left).Trim();
                int right = value.IndexOf(')');
                string sCols = value.Substring(left + 1, right - left - 1);

                columns = sCols.Split(',');
                for (int i = 0; i < columns.Length; i++)
                {
                    columns[i] = columns[i].Trim();
                }

                return table;
            }

            string[] ss = foreignKey.Split('-');

            string[] cols;
            Table = Split(ss[0], out cols);
            Columns = cols;

            string[] relatedCols;
            RelatedTable = Split(ss[1], out relatedCols);
            RelatedColumns = relatedCols;
        }

        public override string ToString()
        {
            return string.Format("{0}({1})-{2}({3})",
                 Table, string.Join(",", Columns),
                 RelatedTable, string.Join(",", RelatedColumns));
        }


    }
}
