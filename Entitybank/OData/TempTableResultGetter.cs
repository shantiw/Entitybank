using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XData.Data.DataObjects;
using XData.Data.Objects;
using XData.Data.Schema;

namespace XData.Data.OData
{
    public abstract partial class TempTableResultGetter : QueryExpandResultGetter
    {
        public TempTableResultGetter(Database database) : base(database)
        {
        }

        public override ResultNode GetCollection(QueryExpand queryExpand)
        {
            TempTableNode root = BuildTempTableTree(queryExpand);

            Database.Connection.Open();
            try
            {
                Execute(root);
            }
            finally
            {
                Database.Connection.Close();
            }

            return root.ToResultNode();
        }

        protected TempTableNode BuildTempTableTree(QueryExpand queryExpand)
        {
            Query query = queryExpand.Query;

            XElement entitySchema = query.Schema.GetEntitySchema(query.Entity);
            string name = entitySchema.Attribute(SchemaVocab.Collection).Value;

            string[] select = new string[query.Select.Properties.Length];
            query.Select.Properties.CopyTo(select, 0);
            Order[] orderby = GetOrders(query);
            TempTableNode root = new CollectionTempNode(name, select, orderby, query.Entity) { Path = name };

            //
            Dictionary<QueryNode, TempTableNode> childrenDict = CreateChildren(queryExpand.Nodes, root, out IEnumerable<string> relatedPropertiesForSelect);
            query.Select.Properties = query.Select.Properties.Union(relatedPropertiesForSelect).ToArray();

            //
            if (query.Top != 0 || query.Skip != 0)
            {
                SetRootPagingStatments(root, queryExpand);
            }
            else
            {
                SetRootSelectStatments(root, queryExpand);
            }

            //
            foreach (KeyValuePair<QueryNode, TempTableNode> pair in childrenDict)
            {
                pair.Value.ParentTempTableName = root.TempTableName;
                Compose(pair.Key, pair.Value);
            }

            return root;
        }

        // overload
        protected Dictionary<QueryNode, TempTableNode> CreateChildren(QueryNode queryNode, TempTableNode node,
            out IEnumerable<string> relatedPropertiesForSelect)
        {
            return CreateChildren(queryNode.Children, node, out relatedPropertiesForSelect);
        }

        protected Dictionary<QueryNode, TempTableNode> CreateChildren(IEnumerable<QueryNode> qChildren, TempTableNode node,
            out IEnumerable<string> relatedPropertiesForSelect)
        {
            Dictionary<QueryNode, TempTableNode> childDict = new Dictionary<QueryNode, TempTableNode>();

            List<string> childrenForSelect = new List<string>();
            foreach (QueryNode qChild in qChildren)
            {
                TempTableNode child;
                string[] select = new string[qChild.Query.Select.Properties.Length];
                qChild.Query.Select.Properties.CopyTo(select, 0);
                if (qChild is EntityQueryNode)
                {
                    child = new EntityTempNode(qChild.Property, select, qChild.Entity);
                }
                else if (qChild is CollectionQueryNode)
                {
                    Order[] orderby = GetOrders(qChild.Query);
                    child = new CollectionTempNode(qChild.Property, select, orderby, qChild.Entity);
                }
                else
                {
                    throw new NotSupportedException(qChild.GetType().ToString()); // never
                }
                child.Path = node.Path + "/" + qChild.Property;

                //
                DirectRelationship firstDirectRelationship = qChild.Relationship.DirectRelationships[0];
                Dictionary<string, string> relatedKey = new Dictionary<string, string>();
                for (int i = 0; i < firstDirectRelationship.Properties.Length; i++)
                {
                    relatedKey.Add(firstDirectRelationship.Properties[i], firstDirectRelationship.RelatedProperties[i]);
                }
                child.RelatedKey = relatedKey;

                node.Children.Add(child);

                // NativeProperties
                childrenForSelect.AddRange(firstDirectRelationship.Properties);

                childDict.Add(qChild, child);
            }

            relatedPropertiesForSelect = childrenForSelect.Distinct();

            return childDict;
        }

        protected virtual Order[] GetOrders(Query query)
        {
            return (query.Orderby == null) ? new Order[0] : query.Orderby.Orders;
        }

        protected abstract void SetRootSelectStatments(TempTableNode root, QueryExpand queryExpand);
        protected abstract void SetRootPagingStatments(TempTableNode root, QueryExpand queryExpand);

        protected void Compose(QueryNode queryNode, TempTableNode node)
        {
            Query query = queryNode.Query;

            Dictionary<QueryNode, TempTableNode> childrenDict = CreateChildren(queryNode, node, out IEnumerable<string> relatedPropertiesForSelect);

            IEnumerable<string> unionSelect = new List<string>(queryNode.Query.Select.Properties);
            unionSelect = unionSelect.Union(relatedPropertiesForSelect);

            IEnumerable<string> keyProperties = GetKeyProperties(query);
            unionSelect = unionSelect.Union(keyProperties);

            foreach (string key in node.RelatedKey.Keys.ToList())
            {
                string prop = node.RelatedKey[key];
                string alias = prop;
                int i = 1;
                while (unionSelect.Contains(alias))
                {
                    alias += i.ToString();
                    i++;
                }
                node.RelatedKey[key] = alias;
            }

            node.DistinctKey = node.RelatedKey.Values.Union(keyProperties);
            queryNode.Query.Select.Properties = unionSelect.ToArray();

            //
            SetNodeStatments(node, queryNode);

            //
            foreach (KeyValuePair<QueryNode, TempTableNode> pair in childrenDict)
            {
                pair.Value.ParentTempTableName = node.TempTableName;
                Compose(pair.Key, pair.Value);
            }
        }

        protected abstract void SetNodeStatments(TempTableNode node, QueryNode queryNode);

        protected void Execute(TempTableNode node)
        {
            ExecuteNode(node);
            ExecuteChildren(node);
        }

        private void ExecuteNode(TempTableNode node)
        {
            foreach (SQLStatment statment in node.BeforeExecuteStatments)
            {
                int i = DatabaseExecuteNonQuery(statment.Sql, statment.Parameters);
            }

            node.Table = Database.ExecuteDataTable(node.FetchTableStatment.Sql, node.FetchTableStatment.Parameters);
            node.Table.TableName = node.Name;
            //Database.RecoverColumnNamesCaseSensitivity(node.Table, node.Select);

            foreach (SQLStatment statment in node.AfterExecuteStatments)
            {
                int i = DatabaseExecuteNonQuery(statment.Sql, statment.Parameters);
            }
        }

        // breadth-first
        private void ExecuteChildren(TempTableNode parent)
        {
            foreach (TempTableNode child in parent.Children)
            {
                ExecuteNode(child);
            }

            foreach (SQLStatment statment in GenerateDropTempTableStatements(parent.TempTableName))
            {
                int i = DatabaseExecuteNonQuery(statment.Sql);
            }

            foreach (TempTableNode child in parent.Children)
            {
                ExecuteChildren(child);
            }
        }

        protected virtual SQLStatment[] GenerateDropTempTableStatements(string tempTableName)
        {
            SQLStatment[] statments = new SQLStatment[1];
            statments[0] = new SQLStatment(string.Format("DROP TABLE {0}", tempTableName));
            return statments;
        }

        protected virtual int DatabaseExecuteNonQuery(string sql, params object[] parameters)
        {
            return Database.ExecuteSqlCommand(sql, parameters);
        }

        protected IEnumerable<string> GenerateInnerJoins(ForeignKey[] foreignKeys, string parentTempTableName)
        {
            const string TableAliasPrefix = "S";

            List<string> innerJoins = new List<string>();

            ForeignKey firstFK = foreignKeys[0];
            firstFK = new ForeignKey(parentTempTableName, firstFK.RelatedTable, firstFK.Columns, firstFK.RelatedColumns);
            foreignKeys[0] = firstFK;

            ForeignKey lastFK = foreignKeys[foreignKeys.Length - 1];
            lastFK.TableAlias = TableAliasPrefix;
            lastFK.RelatedTableAlias = Table.Default_Table_Alias_Prefix; // "T"
            innerJoins.Add(GenerateInnerJoin(lastFK));

            string lastTableAlias = lastFK.TableAlias;
            for (int i = foreignKeys.Length - 2; i >= 0; i--)
            {
                ForeignKey foreignKey = foreignKeys[i];

                foreignKey.RelatedTableAlias = lastTableAlias;

                lastTableAlias = TableAliasPrefix + (foreignKeys.Length - i - 1).ToString();
                foreignKey.TableAlias = lastTableAlias;

                innerJoins.Add(GenerateInnerJoin(foreignKey));
            }

            return innerJoins;
        }

        protected string GenerateInnerJoin(ForeignKey foreignKey)
        {
            List<string> onList = new List<string>();
            for (int i = 0; i < foreignKey.Columns.Length; i++)
            {
                string onString = string.Format("{0} = {1}",
                    DecorateColumnName(foreignKey.Columns[i], foreignKey.TableAlias),
                    DecorateColumnName(foreignKey.RelatedColumns[i], foreignKey.RelatedTableAlias));
                onList.Add(onString);
            }

            return string.Format("INNER JOIN {0} {1} ON {2}",
                DecorateTableName(foreignKey.Table),
                DecorateTableAlias(foreignKey.TableAlias),
                string.Join("AND ", onList));
        }

        protected IEnumerable<string> GetRelatedKeyColProps(TempTableNode node, ForeignKey fistForeignKey, out IEnumerable<string> relatedKeyCols)
        {
            List<string> colProps = new List<string>();
            List<string> cols = new List<string>();
            string[] aliases = node.RelatedKey.Values.ToArray();
            for (int i = 0; i < fistForeignKey.RelatedColumns.Length; i++)
            {
                string decorateColumnName = DecorateColumnName(fistForeignKey.RelatedColumns[i], fistForeignKey.RelatedTableAlias);
                colProps.Add(string.Format("{0} {1}", decorateColumnName, DecorateColumnAlias(aliases[i])));
                cols.Add(decorateColumnName);
            }

            relatedKeyCols = cols;
            return colProps;
        }

        protected IEnumerable<string> GetDistinctGroupByProps(IEnumerable<string> distinctKey)
        {
            List<string> groupbyProps = new List<string>();
            foreach (string prop in distinctKey)
            {
                string decorated = DecorateColumnName(prop);
                groupbyProps.Add(decorated);
            }
            return groupbyProps;
        }

        protected IEnumerable<string> GetDistinctSelectProps(IEnumerable<string> select, Dictionary<string, string> relatedKey)
        {
            List<string> selectProps = new List<string>(select);
            selectProps.AddRange(relatedKey.Values);
            return selectProps.Select(prop => DecorateColumnName(prop));
        }

        // SQL Server, Oracle
        protected string GetRowNumberAlias(string defaultAlias, Select select, Dictionary<string, string> relatedKey)
        {
            string rowNumberAlias = defaultAlias;
            int iSuffix = 1;
            while (select.Properties.Contains(rowNumberAlias) || relatedKey.Values.Contains(rowNumberAlias))
            {
                rowNumberAlias += iSuffix;
                iSuffix++;
            }
            return rowNumberAlias;
        }


    }
}
