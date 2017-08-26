using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.DataObjects
{
    public sealed class Table
    {
        public const string Default_Table_Alias_Prefix = "T";
        public string TableAliasPrefix { get; private set; } = Default_Table_Alias_Prefix;

        public string TableAlias { get => TableAliasPrefix; }

        public string Name { get; private set; }
        public IReadOnlyDictionary<string, Column> Columns { get; private set; }
        public IEnumerable<ForeignKeyNode> ForeignKeyTrees { get; private set; }

        private int _tableAliasCount = 1;
        private string GetNextTableAlias()
        {
            string relatedTableAlias = TableAliasPrefix + _tableAliasCount.ToString();
            _tableAliasCount++;
            return relatedTableAlias;
        }

        public Table(IEnumerable<Property> properties, string entity, XElement schema)
        {
            Dictionary<string, Column> columns = new Dictionary<string, Column>();
            List<ForeignKeyNode> roots = new List<ForeignKeyNode>();

            XElement entitySchema = schema.GetEntitySchema(entity);
            Name = entitySchema.Attribute(SchemaVocab.Table).Value;

            foreach (Property property in properties)
            {
                Column column;
                if (property is NativeProperty)
                {
                    column = (property as NativeProperty).CreateColumn(entity, schema);
                    column.TableAlias = TableAlias;
                }
                else if (property is ExtendProperty)
                {
                    column = (property as ExtendProperty).CreateColumn(schema);
                }
                else
                {
                    throw new NotSupportedException(property.GetType().ToString());
                }
                columns.Add(property.Name, column);

                if (property is ExtendProperty)
                {
                    ForeignKeyNode[] nodes = CreateForeignKeyNodes(property as ExtendProperty, schema);
                    string relatedTableAlias = TableAlias;

                    ICollection<ForeignKeyNode> treeNodes = roots;
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        ForeignKeyNode treeNode = treeNodes.FirstOrDefault(n => n.Key == nodes[i].Key);
                        if (treeNode == null)
                        {
                            nodes[i].ForeignKey.TableAlias = relatedTableAlias;
                            relatedTableAlias = GetNextTableAlias();
                            nodes[i].ForeignKey.RelatedTableAlias = relatedTableAlias;
                            for (int j = i + 1; j < nodes.Length; j++)
                            {
                                nodes[j].ForeignKey.TableAlias = relatedTableAlias;
                                relatedTableAlias = GetNextTableAlias();
                                nodes[j].ForeignKey.RelatedTableAlias = relatedTableAlias;

                                nodes[j - 1].Children.Add(nodes[j]);
                            }
                            treeNodes.Add(nodes[i]);
                            break;
                        }
                        else
                        {
                            relatedTableAlias = treeNode.ForeignKey.RelatedTableAlias;
                            treeNodes = treeNode.Children;
                        }
                    }
                    column.TableAlias = relatedTableAlias;
                }
            }

            Columns = columns;
            ForeignKeyTrees = roots;
        }

        public Table(IEnumerable<Property> properties, string entity, XElement schema, string tableAliasPrefix)
            : this(properties, entity, schema)
        {
            TableAliasPrefix = tableAliasPrefix;
        }

        private ForeignKeyNode[] CreateForeignKeyNodes(ExtendProperty property, XElement schema)
        {
            ForeignKeyNode[] nodes = new ForeignKeyNode[property.Relationship.DirectRelationships.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                ForeignKey foreignKey = property.Relationship.DirectRelationships[i].CreateForeignKey(schema);
                nodes[i] = new ForeignKeyNode(foreignKey);
            }
            return nodes;
        }

    }
}
