using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Modification
{
    public partial class OracleModificationGenerator : ModificationGenerator
    {
        protected override int InsertRowLimit => int.MaxValue;
        protected override int DbParamLimit => 64000;

        //INSERT INTO Employees
        //(Id, Name, ...)
        //SELECT 1, 'Name 1', ... FROM DUAL
        //UNION ALL
        //SELECT 2, 'Name 2', ... FROM DUAL
        //UNION ALL
        //SELECT...
        protected override BatchStatement GenerateBatchInsertStatement(int startIndex, int endIndex,
            Dictionary<string, object>[] array, Dictionary<string, Tuple<string, Type>> propertyColumns, string tableName)
        {
            int index = 0;
            Dictionary<string, object> paramDict = new Dictionary<string, object>();
            List<string> valuesClause = new List<string>();
            for (int i = startIndex; i <= endIndex; i++)
            {
                Dictionary<string, object> propertyValues = array[i];
                List<string> valueList = new List<string>();
                foreach (KeyValuePair<string, Tuple<string, Type>> propertyColumn in propertyColumns)
                {
                    object value = propertyValues[propertyColumn.Key];
                    valueList.Add(Batch_GetValueExpr(value, propertyColumn.Value.Item2, paramDict, index));
                }
                valuesClause.Add(string.Format("SELECT {0} FROM DUAL", string.Join(",", valueList)));
            }

            //
            string sql = string.Format("INSERT INTO {0} ({1}) {2}", tableName,
                string.Join(",", propertyColumns.Values), string.Join(" UNION ALL ", valuesClause));

            return new BatchStatement(sql, paramDict, startIndex, endIndex);
        }

        //DELETE Transcripts
        //WHERE(StudentId, CourseId) IN ...
        public override IEnumerable<BatchStatement> GenerateBatchDeleteStatements(Dictionary<string, object>[] objects,
            XElement entitySchema, XElement keySchema, XElement concurrencySchema)
        {
            string tableName = DecorateTableName(entitySchema.Attribute(SchemaVocab.Table).Value);

            List<BatchStatement> result = new List<BatchStatement>();
            IEnumerable<BatchStatement> whereInClauses = Batch_GenerateWhereINClauses(objects, keySchema, concurrencySchema, DbParamLimit);
            foreach (BatchStatement statement in whereInClauses)
            {
                string sql = string.Format("DELETE {0} {1}", tableName, statement.Sql);
                result.Add(new BatchStatement(sql, statement.Parameters, statement.StartIndex, statement.EndIndex));
            }
            return result;
        }

        //UPDATE Transcripts
        //SET Score = 90,
        //Levle='A'
        //WHERE(StudentId, CourseId) IN ...
        public override IEnumerable<BatchStatement> GenerateBatchUpdateStatements(Dictionary<string, object>[] objects,
            Dictionary<string, object> value, XElement entitySchema, XElement keySchema, XElement concurrencySchema)
        {
            int dbParamIndex = DbParamLimit;
            Dictionary<string, object> paramDict = new Dictionary<string, object>();
            List<string> valueList = new List<string>();
            foreach (string property in value.Keys)
            {
                XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).First(p => p.Attribute(SchemaVocab.Name).Value == property);
                string column = propertySchema.Attribute(SchemaVocab.Column).Value;
                column = DecorateColumnName(column);
                Type type = Type.GetType(propertySchema.Attribute(SchemaVocab.DataType).Value);
                string valueExpr = Batch_GetValueExpr(value[property], type, paramDict, dbParamIndex);
                valueList.Add(string.Format("{0} = {1}", column, valueExpr));
                dbParamIndex--;
            }

            int dbParamLimit = DbParamLimit - paramDict.Count;
            IEnumerable<BatchStatement> whereInClauses = Batch_GenerateWhereINClauses(objects, keySchema, concurrencySchema, dbParamLimit);

            string tableName = DecorateTableName(entitySchema.Attribute(SchemaVocab.Table).Value);
            string head = string.Format("UPDATE {0} SET {1} ", tableName, string.Join(",", valueList));

            List<BatchStatement> result = new List<BatchStatement>();
            foreach (BatchStatement statement in whereInClauses)
            {
                result.Add(new BatchStatement(head + statement.Sql, statement.Parameters, statement.StartIndex, statement.EndIndex));
            }

            return result;
        }

        //WHERE(StudentId, CourseId) IN
        //(
        // (1,1),
        // (2,1),
        //...
        //)
        protected IEnumerable<BatchStatement> Batch_GenerateWhereINClauses(Dictionary<string, object>[] objects,
            XElement keySchema, XElement concurrencySchema, int dbParamLimit)
        {
            XElement keyConcSchema = new XElement(keySchema);
            if (concurrencySchema != null)
            {
                keyConcSchema.Add(concurrencySchema.Elements());
            }

            Dictionary<string, Tuple<string, Type>> propertyColumns = Batch_GetKeyConcPropertyColumns(keyConcSchema);

            IEnumerable<string> columns = propertyColumns.Select(pc => pc.Value.Item1);
            string sColumns = string.Join(",", columns);

            int nonIntColCount = propertyColumns.Values.Where(v => IsInteger(v.Item2)).Count();
            int pageSize;
            if (nonIntColCount == 0)
            {
                pageSize = ListExprLimit;
            }
            else
            {
                pageSize = dbParamLimit / nonIntColCount;
                if (dbParamLimit % nonIntColCount != 0) pageSize++;

                pageSize = (pageSize < ListExprLimit) ? pageSize : ListExprLimit;
            }

            List<BatchStatement> statements = new List<BatchStatement>();
            Dictionary<int, int> partitions = Batch_Paging(objects.Length, pageSize);
            foreach (KeyValuePair<int, int> partition in partitions)
            {
                int startIndex = partition.Key;
                int endIndex = partition.Value;
                int dbParamIndex = 1;
                Dictionary<string, object> paramDict = new Dictionary<string, object>();
                List<string> exprs = new List<string>();
                for (int i = startIndex; i <= endIndex; i++)
                {
                    Dictionary<string, object> propertyValues = objects[i];
                    List<string> values = new List<string>();
                    foreach (KeyValuePair<string, Tuple<string, Type>> propertyColumn in propertyColumns)
                    {
                        string property = propertyColumn.Key;
                        string column = propertyColumn.Value.Item1;
                        Type type = propertyColumn.Value.Item2;
                        object value = propertyValues[property];
                        if (IsInteger(type))
                        {
                            values.Add(value.ToString());
                        }
                        else
                        {
                            string dbParameterName = GetDbParameterName(dbParamIndex);
                            paramDict.Add(dbParameterName, value);
                            values.Add(dbParameterName);
                            dbParamIndex++;
                        }
                    }
                    string valuesExpr = string.Format("({0})", string.Join(",", values));
                    exprs.Add(valuesExpr);
                }
                string where = string.Format("WHERE({0}) IN ({1})", sColumns, string.Join(",", exprs));
                statements.Add(new BatchStatement(where, paramDict, startIndex, endIndex));
            }

            return statements;
        }

        //UPDATE Users S
        //SET(IsDisabled, IsLockedOut)=(SELECT T.IsDisabled, T.IsLockedOut
        //FROM
        //(
        //SELECT 1 EmployeeId, 0 IsDisabled, 1 IsLockedOut FROM DUAL
        //UNION ALL
        //SELECT 2 EmployeeId, 1 IsDisabled, 0 IsLockedOut FROM DUAL
        //) T
        //WHERE S.EmployeeId = T.EmployeeId)
        public override IEnumerable<BatchStatement> GenerateBatchUpdateStatements(Dictionary<string, object>[] objects,
            XElement entitySchema, XElement keySchema, XElement concurrencySchema)
        {
            string tableName = DecorateTableName(entitySchema.Attribute(SchemaVocab.Table).Value);
            Dictionary<string, Tuple<string, Type>> propertyColumns = Batch_GetPropertyColumns(objects[0], entitySchema);

            //
            XElement keyConcSchema = new XElement(keySchema);
            if (concurrencySchema != null)
            {
                keyConcSchema.Add(concurrencySchema.Elements());
            }
            IEnumerable<string> keyConcCols = keyConcSchema.Elements(SchemaVocab.Property)
                .Select(x => DecorateColumnName(x.Attribute(SchemaVocab.Column).Value))
                .Select(s => string.Format("S.{0} = T.{0}", s));
            string where = string.Format("WHERE {0})", string.Join(" AND ", keyConcCols));

            //
            List<string> updateCols = new List<string>();
            foreach (KeyValuePair<string, Tuple<string, Type>> propertyColumn in propertyColumns)
            {
                string property = propertyColumn.Key;
                string column = propertyColumn.Value.Item1;
                if (keyConcSchema.Elements(SchemaVocab.Property).Any(x => x.Attribute(SchemaVocab.Name).Value == propertyColumn.Key)) continue;
                updateCols.Add(column);
            }
            string update = string.Format("UPDATE {0} S SET(IsDisabled, IsLockedOut) = (SELECT T.IsDisabled, T.IsLockedOut FROM (",
                tableName, string.Join(",", updateCols), string.Join(",", updateCols.Select(s => string.Format("T.{0}", s))));

            //
            int nonIntColCount = propertyColumns.Values.Where(v => IsInteger(v.Item2)).Count();
            int pageSize;
            if (nonIntColCount == 0)
            {
                pageSize = objects.Length;
            }
            else
            {
                pageSize = DbParamLimit / nonIntColCount;
                if (DbParamLimit % nonIntColCount != 0) pageSize++;
            }

            //
            List<BatchStatement> result = new List<BatchStatement>();
            Dictionary<int, int> partitions = Batch_Paging(objects.Length, pageSize);
            foreach (KeyValuePair<int, int> partition in partitions)
            {
                BatchStatement statement = GenerateBatchUpdateStatement(partition.Key, partition.Value, objects, propertyColumns, update, where);
                result.Add(statement);
            }
            return result;
        }

        protected BatchStatement GenerateBatchUpdateStatement(int startIndex, int endIndex,
            Dictionary<string, object>[] array, Dictionary<string, Tuple<string, Type>> propertyColumns, string head, string tail)
        {
            int dbParamIndex = 1;
            Dictionary<string, object> paramDict = new Dictionary<string, object>();
            List<string> selects = new List<string>();
            for (int i = startIndex; i <= endIndex; i++)
            {
                Dictionary<string, object> propertyValues = array[i];
                List<string> valueList = new List<string>();
                foreach (KeyValuePair<string, Tuple<string, Type>> propertyColumn in propertyColumns)
                {
                    string property = propertyColumn.Key;
                    string column = propertyColumn.Value.Item1;
                    Type type = propertyColumn.Value.Item2;
                    object value = propertyValues[property];
                    string valueExpr = Batch_GetValueExpr(value, type, paramDict, dbParamIndex);
                    valueList.Add(string.Format("{0} {1}", column, valueExpr));
                    dbParamIndex++;
                }
                selects.Add(string.Format("SELECT {0} FROM DUAL", string.Join(",", valueList)));
            }

            string sql = string.Format("{0} ({1}) T {2}", head, string.Join(" UNION ALL ", selects), tail);
            return new BatchStatement(sql, paramDict, startIndex, endIndex);
        }


    }
}
