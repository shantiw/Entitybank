using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Helpers;
using XData.Data.Schema;

namespace XData.Data.Modification
{
    public abstract partial class ModificationGenerator
    {
        protected virtual int InsertRowLimit => 1000; // performance
        protected virtual int ListExprLimit => 1000; // syntax
        protected abstract int DbParamLimit { get; } // syntax

        public virtual IEnumerable<BatchStatement> GenerateBatchInsertStatements(Dictionary<string, object>[] objects, XElement entitySchema)
        {
            string tableName = DecorateTableName(entitySchema.Attribute(SchemaVocab.Table).Value);
            Dictionary<string, Tuple<string, Type>> propertyColumns = Batch_GetPropertyColumns(objects[0], entitySchema);

            int nonIntColCount = propertyColumns.Values.Where(v => IsInteger(v.Item2)).Count();
            int pageSize;
            if (nonIntColCount == 0)
            {
                pageSize = InsertRowLimit;
            }
            else
            {
                pageSize = DbParamLimit / nonIntColCount;
                if (DbParamLimit % nonIntColCount != 0) pageSize++;

                pageSize = (pageSize < InsertRowLimit) ? pageSize : InsertRowLimit;
            }

            Dictionary<int, int> partitions = Batch_Paging(objects.Length, pageSize);

            List<BatchStatement> result = new List<BatchStatement>();
            foreach (KeyValuePair<int, int> partition in partitions)
            {
                result.Add(GenerateBatchInsertStatement(partition.Key, partition.Value, objects, propertyColumns, tableName));
            }

            return result;
        }

        protected Dictionary<string, Tuple<string, Type>> Batch_GetPropertyColumns(Dictionary<string, object> propertyValues, XElement entitySchema)
        {
            Dictionary<string, Tuple<string, Type>> propertyColumns = new Dictionary<string, Tuple<string, Type>>();
            foreach (string property in propertyValues.Keys)
            {
                XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).First(p => p.Attribute(SchemaVocab.Name).Value == property);
                string column = propertySchema.Attribute(SchemaVocab.Column).Value;
                Type type = Type.GetType(propertySchema.Attribute(SchemaVocab.DataType).Value);
                propertyColumns.Add(property, Tuple.Create(DecorateColumnName(column), type));
            }
            return propertyColumns;
        }

        protected static Dictionary<int, int> Batch_Paging(int count, int pageSize)
        {
            int pageCount = count / pageSize;

            // startIndex, endIndex
            Dictionary<int, int> partitions = new Dictionary<int, int>();
            for (int i = 0; i < pageCount; i++)
            {
                partitions.Add(i * pageSize, (i + 1) * pageSize - 1);
            }
            int remainder = count % pageSize;
            if (remainder != 0)
            {
                partitions.Add(pageCount * pageSize, remainder);
            }

            return partitions;
        }

        // SQL Server/MySQL
        //INSERT INTO Employees
        //(Id, Name, ...)
        //VALUES
        //(1, 'Name 1', ...),
        //(2, 'Name 2', ...)...
        protected virtual BatchStatement GenerateBatchInsertStatement(int startIndex, int endIndex,
            Dictionary<string, object>[] array, Dictionary<string, Tuple<string, Type>> propertyColumns, string tableName)
        {
            int dbParamIndex = 1;
            Dictionary<string, object> paramDict = new Dictionary<string, object>();
            List<string> valuesClause = new List<string>();
            for (int i = startIndex; i <= endIndex; i++)
            {
                Dictionary<string, object> propertyValues = array[i];
                List<string> valueList = new List<string>();
                foreach (KeyValuePair<string, Tuple<string, Type>> propertyColumn in propertyColumns)
                {
                    string property = propertyColumn.Key;
                    Type type = propertyColumn.Value.Item2;
                    object value = propertyValues[property];
                    valueList.Add(Batch_GetValueExpr(value, type, paramDict, dbParamIndex));
                    dbParamIndex++;
                }
                valuesClause.Add(string.Format("({0})", string.Join(",", valueList)));
            }

            //
            string sql = string.Format("INSERT INTO {0} ({1}) VALUES({2})", tableName,
                string.Join(",", propertyColumns.Values.Select(v => v.Item1)), string.Join(",", valuesClause));

            return new BatchStatement(sql, paramDict, startIndex, endIndex);
        }

        protected string Batch_GetValueExpr(object value, Type type, Dictionary<string, object> paramDict, int dbParamIndex)
        {
            if (value == null || value == DBNull.Value)
            {
                return "NULL";
            }
            else
            {
                if (IsInteger(type))
                {
                    if (value is bool)
                    {
                        return (bool)value ? "1" : "0";
                    }
                    else
                    {
                        return value.ToString();
                    }
                }
                else
                {
                    string dbParameterName = GetDbParameterName(dbParamIndex);
                    paramDict.Add(dbParameterName, value);
                    return dbParameterName;
                }
            }
        }

        // SQL Server/MySQL
        //DELETE Transcripts
        //WHERE Id IN (1, 2...)
        // or
        //DELETE S
        //...
        public virtual IEnumerable<BatchStatement> GenerateBatchDeleteStatements(Dictionary<string, object>[] objects,
            XElement entitySchema, XElement keySchema, XElement concurrencySchema)
        {
            IEnumerable<BatchStatement> whereClauses = Batch_GenerateWhereClauses(objects, entitySchema, keySchema, concurrencySchema, DbParamLimit, out string alias);
            IEnumerable<BatchStatement> result = whereClauses;

            string head = string.IsNullOrWhiteSpace(alias) ? "DELETE " : "DELETE " + alias + " ";

            foreach (BatchStatement statement in result)
            {
                statement.SetSql(head + statement.Sql);
            }

            return result;
        }

        // SQL Server/MySQL
        //UPDATE Transcripts
        //SET Score = 90,
        //Levle='A'
        //WHERE Id IN (1, 2...)
        // or
        //UPDATE S
        //SET Score = 90,
        //Levle='A'
        //...
        public virtual IEnumerable<BatchStatement> GenerateBatchUpdateStatements(Dictionary<string, object>[] objects,
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
            IEnumerable<BatchStatement> whereClauses = Batch_GenerateWhereClauses(objects, entitySchema, keySchema, concurrencySchema, dbParamLimit, out string alias);
            IEnumerable<BatchStatement> result = whereClauses;

            string tableName = DecorateTableName(entitySchema.Attribute(SchemaVocab.Table).Value);
            string head = string.IsNullOrWhiteSpace(alias) ? string.Format("UPDATE {0} ", tableName) : string.Format("UPDATE {0} ", alias);
            head += string.Format("SET {0} ", string.Join(",", valueList));

            foreach (BatchStatement statement in result)
            {
                statement.SetSql(head + statement.Sql);
            }

            return result;
        }

        protected IEnumerable<BatchStatement> Batch_GenerateWhereClauses(Dictionary<string, object>[] array,
            XElement entitySchema, XElement keySchema, XElement concurrencySchema, int dbParamLimit, out string alias)
        {
            IEnumerable<BatchStatement> whereClauses;

            if (concurrencySchema == null && keySchema.Elements(SchemaVocab.Property).Count() == 1) // IN
            {
                XElement keyPropertySchema = keySchema.Elements().First();
                Type type = Type.GetType(keyPropertySchema.Attribute(SchemaVocab.DataType).Value);
                int pageSize = IsInteger(type) ? ListExprLimit : (dbParamLimit < ListExprLimit ? dbParamLimit : ListExprLimit);

                alias = null;
                whereClauses = Batch_GenerateWhereINClauses(array, pageSize, keyPropertySchema);
            }
            else // SELECT...UNION ALL...
            {
                int nonIntColCount = 0;
                XElement KeyConcSchema = new XElement(keySchema);
                foreach (XElement propSchema in keySchema.Elements())
                {
                    Type type = Type.GetType(propSchema.Attribute(SchemaVocab.DataType).Value);
                    if (!IsInteger(type)) nonIntColCount++;
                }
                if (concurrencySchema != null)
                {
                    foreach (XElement propSchema in concurrencySchema.Elements())
                    {
                        Type type = Type.GetType(propSchema.Attribute(SchemaVocab.DataType).Value);
                        if (!IsInteger(type)) nonIntColCount++;
                        KeyConcSchema.Add(propSchema);
                    }
                }

                int pageSize;
                if (nonIntColCount == 0)
                {
                    pageSize = array.Length;
                }
                else
                {
                    pageSize = dbParamLimit / nonIntColCount;
                    if (dbParamLimit % nonIntColCount != 0) pageSize++;
                }

                alias = "S";

                string tableName = DecorateTableName(entitySchema.Attribute(SchemaVocab.Table).Value);
                whereClauses = Batch_GenerateFromSelectWhereClauses(array, pageSize, KeyConcSchema, tableName);
            }

            return whereClauses;
        }

        //WHERE Id IN (1, 2...) // SQL Server/MySQL
        protected IEnumerable<BatchStatement> Batch_GenerateWhereINClauses(Dictionary<string, object>[] array,
            int pageSize, XElement keyPropertySchema)
        {
            List<BatchStatement> whereClauses = new List<BatchStatement>();

            string property = keyPropertySchema.Attribute(SchemaVocab.Name).Value;
            string column = keyPropertySchema.Attribute(SchemaVocab.Column).Value;
            Type type = Type.GetType(keyPropertySchema.Attribute(SchemaVocab.DataType).Value);
            column = DecorateColumnName(column);

            Dictionary<int, int> partitions = Batch_Paging(array.Length, pageSize);

            if (IsInteger(type))
            {
                foreach (KeyValuePair<int, int> partition in partitions)
                {
                    int destLength = partition.Value - partition.Key;
                    Dictionary<string, object>[] destArray = new Dictionary<string, object>[destLength];
                    Array.Copy(array, partition.Key, destArray, 0, destLength);
                    IEnumerable<string> values = destArray.Select(dict => dict[property].ToString());
                    string where = string.Format("WHERE {0} IN ({1})", column, string.Join(",", values));
                    IReadOnlyDictionary<string, object> dbParameters = new Dictionary<string, object>();
                    whereClauses.Add(new BatchStatement(where, dbParameters, partition.Key, partition.Value));
                }
            }
            else
            {
                foreach (KeyValuePair<int, int> partition in partitions)
                {
                    int destLength = partition.Value - partition.Key;
                    Dictionary<string, object>[] destArray = new Dictionary<string, object>[destLength];
                    Array.Copy(array, partition.Key, destArray, 0, destLength);

                    //
                    List<string> paramList = new List<string>();
                    Dictionary<string, object> paramDict = new Dictionary<string, object>();
                    for (int i = 0; i < destLength; i++)
                    {
                        string dbParameterName = GetDbParameterName(i + 1);
                        paramList.Add(dbParameterName);
                        paramDict.Add(dbParameterName, destArray[i][property]);
                    }
                    string whereClause = string.Format("WHERE {0} IN {{1}}", column, string.Join(",", paramList));
                    IReadOnlyDictionary<string, object> dbParameters = paramDict;
                    whereClauses.Add(new BatchStatement(whereClause, dbParameters, partition.Key, partition.Value));
                }
            }

            return whereClauses;
        }

        protected IEnumerable<BatchStatement> Batch_GenerateFromSelectWhereClauses(Dictionary<string, object>[] array,
            int pageSize, XElement KeyConcSchema, string tableName)
        {
            List<BatchStatement> whereClauses = new List<BatchStatement>();

            Dictionary<int, int> partitions = Batch_Paging(array.Length, pageSize);
            foreach (KeyValuePair<int, int> partition in partitions)
            {
                BatchStatement statement = Batch_GenerateFromSelectWhereClause(partition.Key, partition.Value, array,
                    Batch_GetKeyConcPropertyColumns(KeyConcSchema), tableName);
                whereClauses.Add(statement);
            }

            return whereClauses;
        }

        //...
        //FROM Transcripts S,
        //(
        //SELECT 1 StudentId, 1 CourseId
        //UNION ALL
        //SELECT 2 StudentId, 1 CourseId
        //...
        //) T
        //WHERE S.StudentId = T.StudentId
        //AND S.CourseId = T.CourseId  // SQL Server/MySQL
        protected BatchStatement Batch_GenerateFromSelectWhereClause(int startIndex, int endIndex,
            Dictionary<string, object>[] array, Dictionary<string, Tuple<string, Type>> keyConcPropertyColumns, string tableName)
        {
            BatchStatement statement = Batch_GenerateSelectUnionAll(startIndex, endIndex, array, keyConcPropertyColumns);

            string sql = string.Format("FROM {0} S, ({1}) T WHERE {2}", tableName, statement.Sql,
                string.Join(" AND ", keyConcPropertyColumns.Select(p => string.Format("S.{0} = T.{0}", p.Value.Item1))));

            statement.SetSql(sql);
            return statement;
        }

        protected BatchStatement Batch_GenerateSelectUnionAll(int startIndex, int endIndex,
            Dictionary<string, object>[] array, Dictionary<string, Tuple<string, Type>> propertyColumns)
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
                selects.Add(string.Format("SELECT {0}", string.Join(",", valueList)));
            }

            return new BatchStatement(string.Join(" UNION ALL ", selects), paramDict, startIndex, endIndex);
        }

        protected Dictionary<string, Tuple<string, Type>> Batch_GetKeyConcPropertyColumns(XElement KeyConcSchema)
        {
            Dictionary<string, Tuple<string, Type>> propertyColumns = new Dictionary<string, Tuple<string, Type>>();
            foreach (XElement propertySchema in KeyConcSchema.Elements())
            {
                string property = propertySchema.Element(SchemaVocab.Property).Value;
                string column = propertySchema.Attribute(SchemaVocab.Column).Value;
                Type type = Type.GetType(propertySchema.Attribute(SchemaVocab.DataType).Value);
                propertyColumns.Add(property, Tuple.Create(DecorateColumnName(column), type));
            }
            return propertyColumns;
        }

        // SQL Server/MySQL
        //UPDATE S
        //SET IsDisabled = T.IsDisabled,
        //IsLockedOut = T.IsLockedOut
        //FROM Users S, 
        //(
        //SELECT 1 EmployeeId, 0 IsDisabled, 1 IsLockedOut
        //UNION ALL
        //SELECT 2 EmployeeId, 1 IsDisabled, 0 IsLockedOut
        //) T
        //WHERE S.EmployeeId = T.EmployeeId
        public virtual IEnumerable<BatchStatement> GenerateBatchUpdateStatements(Dictionary<string, object>[] objects,
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

            List<string> headList = new List<string>();
            List<string> tailList = new List<string>();
            foreach (KeyValuePair<string, Tuple<string, Type>> propertyColumn in propertyColumns)
            {
                string property = propertyColumn.Key;
                string column = propertyColumn.Value.Item1;
                if (keyConcSchema.Elements(SchemaVocab.Property).Any(x => x.Attribute(SchemaVocab.Name).Value == property))
                {
                    tailList.Add(string.Format("S.{0} = T.{0}", column));
                }
                else
                {
                    headList.Add(string.Format("{0} = T.{0}", column));
                }
            }

            string head = string.Format("UPDATE S SET {0} FROM {1} S,", string.Join(",", headList), tableName);
            string tail = string.Format("WHERE {0}", string.Join(",", tailList));

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

            List<BatchStatement> result = new List<BatchStatement>();
            Dictionary<int, int> partitions = Batch_Paging(objects.Length, pageSize);
            foreach (KeyValuePair<int, int> partition in partitions)
            {
                BatchStatement statement = Batch_GenerateSelectUnionAll(partition.Key, partition.Value, objects, propertyColumns);
                statement.SetSql(string.Format("{0} ({1}) T {2}", head, statement.Sql, tail));
                result.Add(statement);
            }

            return result;
        }

        protected static bool IsInteger(Type type)
        {
            return TypeHelper.IsInteger(type);
        }


    }
}
