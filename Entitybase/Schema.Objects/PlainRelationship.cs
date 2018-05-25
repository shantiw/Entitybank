using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    public abstract class PlainRelationship : Relationship
    {
        protected PlainRelationship(string entity, string relatedEntity, IEnumerable<DirectRelationship> relationships)
             : base(entity, relatedEntity, relationships.ToArray())
        {
        }

        protected PlainRelationship(IEnumerable<DirectRelationship> relationships)
            : base(relationships)
        {
        }

        protected PlainRelationship(string relationship, string entity, string relatedEntity, XElement schema)
            : base(entity, relatedEntity, new DirectRelationship[0])
        {
            IEnumerable<PlainRelationship> plainRelationships = Split(relationship, schema, this.GetType());
            plainRelationships = Connect(plainRelationships, relationship);
            DirectRelationships = GetDirectRelationships(plainRelationships);

            Check();
        }

        private static IEnumerable<PlainRelationship> Split(string relationship, XElement schema, Type type)
        {
            List<PlainRelationship> list = new List<PlainRelationship>();

            string[] relationships = Split(relationship);
            foreach (string value in relationships)
            {
                PlainRelationship oPlainRelationship;

                XElement relationshipSchema = schema.GetRelationshipSchema(value);
                if (relationshipSchema == null)
                {
                    if (type == typeof(ManyToOneRelationship))
                    {
                        oPlainRelationship = Create<ManyToOneRelationship>(value);
                    }
                    else if (type == typeof(OneToManyRelationship))
                    {
                        oPlainRelationship = Create<OneToManyRelationship>(value);
                    }
                    else if (type == typeof(OneToOneRelationship))
                    {
                        oPlainRelationship = Create<OneToOneRelationship>(value);
                    }
                    else
                    {
                        throw new NotSupportedException(type.ToString());
                    }
                }
                else
                {
                    Relationship oRelationship = Create(relationshipSchema);
                    oPlainRelationship = oRelationship as PlainRelationship;
                    if (oPlainRelationship == null) throw new NotSupportedException(oRelationship.GetType().ToString());

                    if (type == typeof(ManyToOneRelationship))
                    {
                        if (oPlainRelationship is OneToManyRelationship)
                        {
                            oPlainRelationship = (ManyToOneRelationship)oPlainRelationship.Reverse();
                        }
                    }
                    else if (type == typeof(OneToManyRelationship))
                    {
                        if (oPlainRelationship is ManyToOneRelationship)
                        {
                            oPlainRelationship = (OneToManyRelationship)oPlainRelationship.Reverse();
                        }
                    }
                }
                list.Add(oPlainRelationship);
            }

            return list;
        }

        protected static IEnumerable<PlainRelationship> Connect(IEnumerable<PlainRelationship> relationships, string relationship)
        {
            List<PlainRelationship> decreasing = new List<PlainRelationship>(relationships);

            List<PlainRelationship> list = new List<PlainRelationship>();

            PlainRelationship first = decreasing.First();
            decreasing.Remove(first);
            list.Add(first);

            Connect(first, decreasing, list);

            if (list.Count < relationships.Count()) throw new SchemaException(string.Format(SchemaMessages.RelationshipsNonConnected, relationship));

            return list;
        }

        private static void Connect(PlainRelationship relationship, List<PlainRelationship> decreasing, List<PlainRelationship> list)
        {
            PlainRelationship prev = decreasing.FirstOrDefault(r => r.RelatedEntity == relationship.Entity);
            if (prev == null)
            {
                PlainRelationship rel = decreasing.FirstOrDefault(r => r.Entity == relationship.Entity);
                if (rel is OneToOneRelationship)
                {
                    prev = (PlainRelationship)rel.Reverse();
                    decreasing.Remove(rel);
                }
            }
            else
            {
                decreasing.Remove(prev);
            }
            if (prev != null)
            {
                int index = list.IndexOf(relationship);
                list.Insert(index, prev);
                Connect(prev, decreasing, list);
            }

            PlainRelationship next = decreasing.FirstOrDefault(r => r.Entity == relationship.RelatedEntity);
            if (next == null)
            {
                PlainRelationship rel = decreasing.FirstOrDefault(r => r.RelatedEntity == relationship.RelatedEntity);
                if (rel is OneToOneRelationship)
                {
                    next = (PlainRelationship)rel.Reverse();
                    decreasing.Remove(rel);
                }
            }
            else
            {
                decreasing.Remove(next);
            }
            if (next != null)
            {
                list.Add(next);
                Connect(next, decreasing, list);
            }
        }

    }

    public sealed class ManyToOneRelationship : PlainRelationship
    {
        internal ManyToOneRelationship(string entity, string relatedEntity, IEnumerable<DirectRelationship> relationships)
            : base(entity, relatedEntity, relationships)
        {
        }

        public ManyToOneRelationship(IEnumerable<ManyToOneDirectRelationship> relationships)
            : base(relationships)
        {
        }

        public ManyToOneRelationship(string relationship, string entity, string relatedEntity, XElement schema)
            : base(relationship, entity, relatedEntity, schema)
        {
        }

        public ManyToOneRelationship(OneToOneRelationship relationship)
            : this(relationship.Entity, relationship.RelatedEntity, relationship.DirectRelationships)
        {
        }

        public ManyToOneRelationship(OneToManyRelationship relationship)
            : base(relationship.RelatedEntity, relationship.Entity, ((ManyToOneRelationship)relationship.Reverse()).DirectRelationships)
        {
        }

    }

    public sealed class OneToManyRelationship : PlainRelationship
    {
        internal OneToManyRelationship(string entity, string relatedEntity, IEnumerable<DirectRelationship> relationships)
            : base(entity, relatedEntity, relationships)
        {
        }

        public OneToManyRelationship(IEnumerable<OneToManyDirectRelationship> relationships)
            : base(relationships)
        {
        }

        public OneToManyRelationship(string relationship, string entity, string relatedEntity, XElement schema)
            : base(relationship, entity, relatedEntity, schema)
        {
        }

        public OneToManyRelationship(ManyToOneRelationship relationship)
            : base(relationship.RelatedEntity, relationship.Entity, ((OneToManyRelationship)relationship.Reverse()).DirectRelationships)
        {
        }

    }

    public sealed class OneToOneRelationship : PlainRelationship
    {
        internal OneToOneRelationship(string entity, string relatedEntity, IEnumerable<DirectRelationship> relationships)
            : base(entity, relatedEntity, relationships)
        {
        }

        public OneToOneRelationship(IEnumerable<OneToOneDirectRelationship> relationships)
            : base(relationships)
        {
        }

        public OneToOneRelationship(string relationship, string entity, string relatedEntity, XElement schema)
            : base(relationship, entity, relatedEntity, schema)
        {
        }

    }

}
