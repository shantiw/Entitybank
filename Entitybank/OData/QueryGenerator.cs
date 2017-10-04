using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.DataObjects;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.OData
{
    public abstract partial class QueryGenerator
    {
        // SELECT T.Id, T.Name, T1.Name UniversityName
        // FROM (SELECT NULL Id, @P1 Name, @P2 UniversityId) T // College
        // LEFT JOIN Universities T1 ON T.UniversityId = T1.Id
        public virtual string GenerateDefaultStatement(Query query, out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            Table table = new Table(query.Properties, query.Entity, query.Schema);

            List<string> list = new List<string>();
            string select = GenerateSelect(query.Select, table);
            list.Add(select);
            string form = GenerateDefaultFrom(query, table, out dbParameterValues);
            list.Add(form);

            return string.Join(" ", list);
        }

        protected string GenerateDefaultFrom(Query query, Table table, out IReadOnlyDictionary<string, object> constants)
        {
            List<string> list = new List<string>();
            Dictionary<string, object> dict = new Dictionary<string, object>();

            int parameterCount = 1;
            XElement entitySchema = query.Schema.GetEntitySchema(query.Entity);
            foreach (XElement propertySchema in entitySchema.Elements(SchemaVocab.Property).Where(x => x.Attribute(SchemaVocab.Column) != null))
            {
                string column = propertySchema.Attribute(SchemaVocab.Column).Value;
                string decoratedColumn = DecorateColumnName(column);
                XElement annotation = propertySchema.Elements(SchemaVocab.Annotation).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == "DefaultValue");
                if (annotation == null)
                {
                    list.Add(string.Format("NULL {0}", decoratedColumn));
                }
                else
                {
                    DefaultValueAttribute attribute = annotation.CreateDefaultValueAttribute(propertySchema);
                    string parameter = "@P" + parameterCount.ToString();
                    parameterCount++;
                    list.Add(string.Format("{0} {1}", parameter, decoratedColumn));
                    Dictionary<string, string> upperParamNameMapping = new Dictionary<string, string>();
                    upperParamNameMapping.Add(parameter, parameter);
                    dict.Add(DecorateParameterName(parameter, upperParamNameMapping), attribute.Value);
                }
            }
            string tableBody = string.Format("(SELECT {0})", string.Join(",", list));

            string form = "FROM " + tableBody;
            if (table.ForeignKeyTrees.Count() == 0)
            {
                form += " T";
            }
            else
            {
                IEnumerable<string> leftJoins = GenerateLeftJoins(table.ForeignKeyTrees);
                form += " " + table.ForeignKeyTrees.First().ForeignKey.TableAlias + " " + string.Join(" ", leftJoins);
            }

            constants = dict;
            return form;
        }

        public virtual string GenerateCountStatement(Query query, out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            SelectClauseCollection clauses = GenerateCountClauseCollection(query, out dbParameterValues);

            List<string> list = new List<string>
            {
                clauses.Select,
                clauses.From,
                string.Join(" ", clauses.LeftJoins),
                clauses.Where ?? string.Empty
            };
            return string.Join(" ", list);
        }

        private SelectClauseCollection GenerateCountClauseCollection(Query query, out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            IReadOnlyDictionary<string, object> constants = new Dictionary<string, object>();
            IReadOnlyDictionary<string, string> paramMapping = new Dictionary<string, string>();

            Table table = new Table(query.Properties, query.Entity, query.Schema);

            SelectClauseCollection clauses = new SelectClauseCollection()
            {
                Select = "SELECT COUNT(*)",
                From = "FROM " + DecorateTableName(table.Name)
            };

            clauses.From += " " + DecorateTableAlias(table.TableAlias);

            clauses.LeftJoins = GenerateLeftJoins(table.ForeignKeyTrees);
            if (query.Filter != null)
            {
                clauses.Where = GenerateWhere(query, table, out constants, out paramMapping);
            }

            dbParameterValues = GetDbParameterValues(constants, paramMapping, query.ParameterValues);
            return clauses;
        }

        public virtual string GenerateSelectStatement(Query query, out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            SelectClauseCollection clauses = GenerateSelectClauseCollection(query, out dbParameterValues);

            List<string> list = new List<string>
            {
                clauses.Select,
                clauses.From,
                string.Join(" ", clauses.LeftJoins),
                clauses.Where ?? string.Empty,
                clauses.OrderBy ?? string.Empty
            };
            return string.Join(" ", list);
        }

        internal protected virtual SelectClauseCollection GenerateSelectClauseCollection(Query query, out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            IReadOnlyDictionary<string, object> constants = new Dictionary<string, object>();
            IReadOnlyDictionary<string, string> paramMapping = new Dictionary<string, string>();

            Table table = new Table(query.Properties, query.Entity, query.Schema);

            SelectClauseCollection clauses = new SelectClauseCollection()
            {
                Select = GenerateSelect(query.Select, table),
                From = "FROM " + DecorateTableName(table.Name)
            };
            clauses.From += " " + DecorateTableAlias(table.TableAlias);

            clauses.LeftJoins = GenerateLeftJoins(table.ForeignKeyTrees);
            if (query.Filter != null)
            {
                clauses.Where = GenerateWhere(query, table, out constants, out paramMapping);
            }
            if (query.Orderby != null)
            {
                clauses.OrderBy = GenerateOrderBy(query.Orderby, table);
            }

            dbParameterValues = GetDbParameterValues(constants, paramMapping, query.ParameterValues);
            return clauses;
        }

        protected static IReadOnlyDictionary<string, object> GetDbParameterValues(IReadOnlyDictionary<string, object> constants,
            IReadOnlyDictionary<string, string> paramMapping, IReadOnlyDictionary<string, object> parameterValues)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            // @P1,:P1
            foreach (KeyValuePair<string, string> pair in paramMapping)
            {
                // :P1, value
                dict.Add(pair.Value, parameterValues[pair.Key]);
            }

            foreach (KeyValuePair<string, object> constant in constants)
            {
                dict.Add(constant.Key, constant.Value);
            }

            return dict;
        }

        protected string GenerateSelect(Select select, Table table)
        {
            List<string> list = new List<string>();
            for (int i = 0; i < select.Properties.Length; i++)
            {
                string property = select.Properties[i];
                Column column = table.Columns[property];
                string decoratedColumn = ToSqlString(column, table);
                list.Add(string.Format("{0} {1}", decoratedColumn, DecorateColumnAlias(property)));
            }
            return "SELECT " + string.Join(",", list);
        }

        protected IEnumerable<string> GenerateLeftJoins(IEnumerable<ForeignKeyNode> foreignKeyTrees)
        {
            List<string> list = new List<string>();
            foreach (ForeignKeyNode tree in foreignKeyTrees)
            {
                AddLeftJoin(tree, list);
            }
            return list;
        }

        private void AddLeftJoin(ForeignKeyNode tree, List<string> list)
        {
            List<string> onList = new List<string>();
            for (int i = 0; i < tree.ForeignKey.Columns.Length; i++)
            {
                string onString = string.Format("{0} = {1}",
                    DecorateColumnName(tree.ForeignKey.Columns[i], tree.ForeignKey.TableAlias),
                    DecorateColumnName(tree.ForeignKey.RelatedColumns[i], tree.ForeignKey.RelatedTableAlias));
                onList.Add(onString);
            }

            list.Add(string.Format("LEFT JOIN {0} {1} ON {2}",
                DecorateTableName(tree.ForeignKey.RelatedTable),
                DecorateTableAlias(tree.ForeignKey.RelatedTableAlias),
                string.Join("AND ", onList)));

            foreach (ForeignKeyNode child in tree.Children)
            {
                AddLeftJoin(child, list);
            }
        }

        protected string GenerateWhere(Query query, Table table, out IReadOnlyDictionary<string, object> constants,
            // @P1, :P1
            out IReadOnlyDictionary<string, string> paramMapping)
        {
            Where where = CreateWhere(query, table);
            constants = where.Constants;
            paramMapping = where.ParamMapping;
            return "WHERE " + where.Clause;
        }

        protected string GenerateOrderBy(Orderby orderby, Table table)
        {
            List<string> list = new List<string>();
            foreach (Order order in orderby.Orders)
            {
                Column column = table.Columns[order.Property];
                if (order is DescendingOrder)
                {
                    list.Add(string.Format("{0} {1}", ToSqlString(column, table), "DESC"));
                }
                else // AscendingOrder
                {
                    list.Add(string.Format("{0} {1}", ToSqlString(column, table), "ASC"));
                }
            }
            return "ORDER BY " + string.Join(",", list);
        }

        protected virtual string ToSqlString(Column column, Table table)
        {
            return DecorateColumnName(column.Name, column.TableAlias);
        }

        protected virtual string DecorateColumnName(string column, string tableAlias)
        {
            return string.Format("{0}.{1}", DecorateTableAlias(tableAlias), DecorateColumnName(column));
        }

        internal protected abstract string DecorateTableName(string table);
        internal protected abstract string DecorateTableAlias(string tableAlias);
        internal protected abstract string DecorateColumnName(string column);
        internal protected abstract string DecorateColumnAlias(string columnAlias);

        protected virtual string DecorateParameterName(string parameter, IReadOnlyDictionary<string, string> upperParamNameMapping)
        {
            return upperParamNameMapping[parameter];
        }

        protected abstract Where CreateWhere(Query query, Table table);

        public virtual string GeneratePagingStatement(Query query, out IReadOnlyDictionary<string, object> dbParameterValues)
        {
            PagingClauseCollection clauses = GeneratePagingClauseCollection(query, out dbParameterValues);

            List<string> list = new List<string>
            {
                clauses.Select,
                clauses.From,
                string.Join(" ", clauses.LeftJoins),
                clauses.Where ?? string.Empty,
                clauses.OrderBy ?? string.Empty
            };
            list.AddRange(clauses.Clauses);
            return string.Join(" ", list);
        }

        internal protected abstract PagingClauseCollection GeneratePagingClauseCollection(Query query, out IReadOnlyDictionary<string, object> dbParameterValues);

    }
}
