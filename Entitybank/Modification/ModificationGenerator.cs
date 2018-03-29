using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Modification
{
    public abstract partial class ModificationGenerator
    {
        public virtual string GenerateInsertStatement(Dictionary<string, object> propertyValues, XElement entitySchema, out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            Dictionary<string, object> paramDict = new Dictionary<string, object>();

            List<string> columnList = new List<string>();
            List<string> valueList = new List<string>();
            int index = 0;
            foreach (KeyValuePair<string, object> propertyValue in propertyValues)
            {
                string property = propertyValue.Key;
                object value = propertyValue.Value;

                XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).First(p => p.Attribute(SchemaVocab.Name).Value == property);
                string column = propertySchema.Attribute(SchemaVocab.Column).Value;
                columnList.Add(DecorateColumnName(column));

                if (value == null)
                {
                    valueList.Add("NULL");
                }
                else
                {
                    string dbParameterName = GetDbParameterName(index);
                    valueList.Add(dbParameterName);
                    paramDict.Add(dbParameterName, value);

                    index++;
                }
            }
            string insert = string.Join(",", columnList);
            string values = string.Join(",", valueList);

            //
            string table = entitySchema.Attribute(SchemaVocab.Table).Value;
            string sql = string.Format("INSERT INTO {0} ({1}) VALUES({2})", DecorateTableName(table), insert, values);

            dbParameterValues = paramDict;
            return sql;
        }

        public virtual string GenerateDeleteStatement(Dictionary<string, object> propertyValues, XElement entitySchema, XElement keySchema, XElement concurrencySchema,
            out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            Dictionary<string, object> paramDict = new Dictionary<string, object>();

            List<string> whereList = new List<string>();
            GenerateWhereItems(propertyValues, keySchema, whereList, paramDict);
            if (concurrencySchema != null)
            {
                GenerateWhereItems(propertyValues, concurrencySchema, whereList, paramDict);
            }
            string where = string.Join(" AND ", whereList);

            //
            string table = entitySchema.Attribute(SchemaVocab.Table).Value;
            string sql = string.Format("DELETE FROM {0} WHERE {1}", DecorateTableName(table), where);

            dbParameterValues = paramDict;
            return sql;
        }

        public virtual string GenerateUpdateStatement(Dictionary<string, object> propertyValues, Dictionary<string, object> updatePropertyValues,
            Dictionary<string, object> originalConcurrencyPropertyValues,
            XElement entitySchema, XElement keySchema, XElement concurrencySchema,
            out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            Dictionary<string, object> paramDict = new Dictionary<string, object>();

            //
            List<string> setList = new List<string>();
            int index = 0;
            foreach (KeyValuePair<string, object> propertyValue in updatePropertyValues)
            {
                string property = propertyValue.Key;
                object value = propertyValue.Value;

                XElement propertySchema = entitySchema.Elements(SchemaVocab.Property).First(p => p.Attribute(SchemaVocab.Name).Value == property);
                string column = propertySchema.Attribute(SchemaVocab.Column).Value;

                if (value == null)
                {
                    setList.Add(string.Format("{0} = NULL", DecorateColumnName(column)));
                }
                else
                {
                    string dbParameterName = GetDbParameterName(index);
                    setList.Add(string.Format("{0} = {1}", DecorateColumnName(column), dbParameterName));
                    paramDict.Add(dbParameterName, value);

                    index++;
                }
            }
            string set = string.Join(",", setList);

            //
            List<string> whereList = new List<string>();
            GenerateWhereItems(propertyValues, keySchema, whereList, paramDict);
            if (concurrencySchema != null)
            {
                GenerateWhereItems(originalConcurrencyPropertyValues, concurrencySchema, whereList, paramDict);
            }
            string where = string.Join(" AND ", whereList);

            //
            string table = entitySchema.Attribute(SchemaVocab.Table).Value;
            string sql = string.Format("UPDATE {0} SET {1} WHERE {2}", DecorateTableName(table), set, where);

            dbParameterValues = paramDict;
            return sql;
        }

        protected void GenerateWhereItems(Dictionary<string, object> propertyValues, XElement whereSchema,
            List<string> whereItems, Dictionary<string, object> dbParameterValues)
        {
            int index = dbParameterValues.Count;
            foreach (XElement propertySchema in whereSchema.Elements(SchemaVocab.Property))
            {
                string property = propertySchema.Attribute(SchemaVocab.Name).Value;
                string column = propertySchema.Attribute(SchemaVocab.Column).Value;

                string dbParameterName = GetDbParameterName(index);
                whereItems.Add(string.Format("{0} = {1}", DecorateColumnName(column), dbParameterName));
                dbParameterValues.Add(dbParameterName, propertyValues[property]);

                index++;
            }
        }

        protected string GetDbParameterName(int index)
        {
            const string Parameter_Name_Prefix = "P";

            return DecorateDbParameterName(Parameter_Name_Prefix + index.ToString());
        }

        public virtual string GenerateFetchStatement(Dictionary<string, object> propertyValues, XElement entitySchema, XElement keySchema,
            out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            List<string> whereList = new List<string>();
            Dictionary<string, object> paramDict = new Dictionary<string, object>();

            GenerateWhereItems(propertyValues, keySchema, whereList, paramDict);
            string where = string.Join(" AND ", whereList);

            //
            string table = entitySchema.Attribute(SchemaVocab.Table).Value;
            string sql = string.Format("SELECT * FROM {0} WHERE {1}", DecorateTableName(table), where);

            dbParameterValues = paramDict;
            return sql;
        }

        public abstract string GenerateHasChildStatement(Dictionary<string, object> propertyValues, XElement entitySchema, XElement keySchema,
             out IReadOnlyDictionary<string, object> dbParameterValues);

        protected abstract string DecorateTableName(string table);
        protected abstract string DecorateColumnName(string column);
        protected abstract string DecorateDbParameterName(string parameter);


    }
}
