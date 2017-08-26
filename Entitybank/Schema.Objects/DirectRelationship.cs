using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    public class DirectRelationship // used as UndirectedDirectRelationship
    {
        public string Entity { get; protected set; }
        public string RelatedEntity { get; protected set; }
        public string[] Properties { get; protected set; }
        public string[] RelatedProperties { get; protected set; }

        internal protected DirectRelationship(string entity, string relatedEntity, IEnumerable<string> properties, IEnumerable<string> relatedProperties)
        {
            Entity = entity;
            RelatedEntity = relatedEntity;
            Properties = properties.ToArray();
            RelatedProperties = relatedProperties.ToArray();
        }

        public override string ToString()
        {
            return string.Format("{0}({1})-{2}({3})",
                Entity, string.Join(",", Properties),
                RelatedEntity, string.Join(",", RelatedProperties));
        }

        public DirectRelationship Reverse()
        {
            DirectRelationship relationship = this;

            if (relationship is ManyToOneDirectRelationship)
            {
                return new OneToManyDirectRelationship(relationship as ManyToOneDirectRelationship);
            }
            else if (relationship is OneToManyDirectRelationship)
            {
                return new ManyToOneDirectRelationship(relationship as OneToManyDirectRelationship);
            }
            else if (relationship is OneToOneDirectRelationship)
            {
                return new OneToOneDirectRelationship(relationship.RelatedEntity, relationship.Entity,
                    relationship.RelatedProperties.ToArray(), relationship.Properties.ToArray());
            }
            else
            {
                throw new NotSupportedException(relationship.GetType().ToString()); // never
            }
        }

        internal static IEnumerable<DirectRelationship> Connect(IEnumerable<DirectRelationship> relationships)
        {
            List<DirectRelationship> rels = new List<DirectRelationship>(relationships);

            List<DirectRelationship> list = new List<DirectRelationship>();

            DirectRelationship rel = rels.First();
            rels.Remove(rel);
            list.Add(rel);

            Connect(rel, rels, list);

            if (list.Count < relationships.Count())
            {
                List<string> connList = new List<string>(list.Select(r => r.Entity))
                {
                    list.Last().RelatedEntity
                };
                List<string> originalList = new List<string>(relationships.Select(r => r.Entity))
                {
                    relationships.Last().RelatedEntity
                };
                List<string> mesgList = new List<string>
                {
                    string.Join("-", connList)
                };
                mesgList.AddRange(originalList.Except(connList));

                throw new SchemaException(string.Format(SchemaMessages.DirectRelationshipsNonConnected, string.Join(",", mesgList)));
            }

            return list;
        }

        private static void Connect(DirectRelationship relationship, List<DirectRelationship> relationships, List<DirectRelationship> list)
        {
            DirectRelationship prev = relationships.FirstOrDefault(r => r.RelatedEntity == relationship.Entity);
            if (prev == null)
            {
                DirectRelationship rel = relationships.FirstOrDefault(r => r.Entity == relationship.Entity);
                if (rel is OneToOneDirectRelationship)
                {
                    prev = rel.Reverse();
                    relationships.Remove(rel);
                }
            }
            else
            {
                relationships.Remove(prev);
            }
            if (prev != null)
            {
                int index = list.IndexOf(relationship);
                list.Insert(index, prev);
                Connect(prev, relationships, list);
            }

            DirectRelationship next = relationships.FirstOrDefault(r => r.Entity == relationship.RelatedEntity);
            if (next == null)
            {
                DirectRelationship rel = relationships.FirstOrDefault(r => r.RelatedEntity == relationship.RelatedEntity);
                if (rel is OneToOneDirectRelationship)
                {
                    next = rel.Reverse();
                    relationships.Remove(rel);
                }
            }
            else
            {
                relationships.Remove(next);
            }
            if (next != null)
            {
                list.Add(next);
                Connect(next, relationships, list);
            }
        }

        internal static DirectRelationship Create(XElement directRelationshipSchema, string type)
        {
            string relType;
            XAttribute attr = directRelationshipSchema.Attribute(SchemaVocab.Type);
            if (attr == null)
            {
                relType = type ?? throw new ArgumentNullException("type");
            }
            else
            {
                relType = attr.Value;
                if (type != null && type != relType) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, type, relType), "type");
            }
            string entity = directRelationshipSchema.Attribute(SchemaVocab.Entity).Value;
            string relatedEntity = directRelationshipSchema.Attribute(SchemaVocab.RelatedEntity).Value;
            IEnumerable<XElement> xProperties = directRelationshipSchema.Elements(SchemaVocab.Property);
            IEnumerable<string> properties = xProperties.Select(x => x.Attribute(SchemaVocab.Name).Value);
            IEnumerable<string> relatedProperties = xProperties.Select(x => x.Attribute(SchemaVocab.RelatedProperty).Value);
            switch (relType)
            {
                case SchemaVocab.ManyToOne:
                    return new ManyToOneDirectRelationship(entity, relatedEntity, properties, relatedProperties);
                case SchemaVocab.OneToMany:
                    return new OneToManyDirectRelationship(entity, relatedEntity, properties, relatedProperties);
                case SchemaVocab.OneToOne:
                    return new OneToOneDirectRelationship(entity, relatedEntity, properties, relatedProperties);
                default:
                    throw new NotSupportedException(relType);
            }
        }

        internal static DirectRelationship Create(XElement directRelationshipSchema)
        {
            return Create(directRelationshipSchema, null);
        }
    }

    public sealed class ManyToOneDirectRelationship : DirectRelationship
    {
        internal ManyToOneDirectRelationship(string entity, string relatedEntity, IEnumerable<string> properties, IEnumerable<string> relatedProperties)
            : base(entity, relatedEntity, properties, relatedProperties)
        {
        }

        public ManyToOneDirectRelationship(OneToOneDirectRelationship relationship)
            : base(relationship.Entity, relationship.RelatedEntity, relationship.Properties.ToArray(), relationship.RelatedProperties.ToArray())
        {
        }

        public ManyToOneDirectRelationship(OneToManyDirectRelationship relationship)
            : base(relationship.RelatedEntity, relationship.Entity, relationship.RelatedProperties.ToArray(), relationship.Properties.ToArray())
        {
        }

    }

    public sealed class OneToManyDirectRelationship : DirectRelationship
    {
        internal OneToManyDirectRelationship(string entity, string relatedEntity, IEnumerable<string> properties, IEnumerable<string> relatedProperties)
            : base(entity, relatedEntity, properties, relatedProperties)
        {
        }

        public OneToManyDirectRelationship(OneToOneDirectRelationship relationship)
            : base(relationship.Entity, relationship.RelatedEntity, relationship.Properties.ToArray(), relationship.RelatedProperties.ToArray())
        {
        }

        public OneToManyDirectRelationship(ManyToOneDirectRelationship relationship)
            : base(relationship.RelatedEntity, relationship.Entity, relationship.RelatedProperties.ToArray(), relationship.Properties.ToArray())
        {
        }

    }

    public sealed class OneToOneDirectRelationship : DirectRelationship
    {
        internal OneToOneDirectRelationship(string entity, string relatedEntity, IEnumerable<string> properties, IEnumerable<string> relatedProperties)
            : base(entity, relatedEntity, properties, relatedProperties)
        {
        }

    }

}
