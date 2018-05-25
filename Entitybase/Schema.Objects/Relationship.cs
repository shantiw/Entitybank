using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    public class Relationship // used as UndirectedRelationship
    {
        public string Name { get; set; }

        public string Entity { get; protected set; }
        public string RelatedEntity { get; protected set; }
        public DirectRelationship[] DirectRelationships { get; protected set; }

        protected Relationship(string entity, string relatedEntity, IEnumerable<DirectRelationship> relationships)
        {
            Entity = entity;
            RelatedEntity = relatedEntity;
            DirectRelationships = relationships.ToArray();
        }

        protected Relationship(IEnumerable<DirectRelationship> relationships)
        {
            IEnumerable<DirectRelationship> rels = DirectRelationship.Connect(relationships);
            Entity = rels.First().Entity;
            RelatedEntity = rels.Last().RelatedEntity;
            DirectRelationships = rels.ToArray();
        }

        internal protected Relationship(string relationship, string entity, string relatedEntity, XElement schema)
        {
            Entity = entity;
            RelatedEntity = relatedEntity;

            IEnumerable<Relationship> relationships = Split(relationship, schema);
            relationships = Connect(relationships, entity, relationship);
            DirectRelationships = GetDirectRelationships(relationships);

            Check();
        }

        private static IEnumerable<Relationship> Split(string relationship, XElement schema)
        {
            List<Relationship> list = new List<Relationship>();

            string[] relationships = Split(relationship);
            foreach (string value in relationships)
            {
                Relationship oRelationship;

                XElement relationshipSchema = schema.GetRelationshipSchema(value);
                if (relationshipSchema == null)
                {
                    oRelationship = Create<Relationship>(value);
                }
                else
                {
                    oRelationship = Create(relationshipSchema);
                }
                list.Add(oRelationship);
            }

            return list;
        }

        protected static DirectRelationship[] GetDirectRelationships(IEnumerable<Relationship> relationships)
        {
            List<DirectRelationship> oDirectRelationships = new List<DirectRelationship>();
            foreach (Relationship oRelationship in relationships)
            {
                oDirectRelationships.AddRange(oRelationship.DirectRelationships);
            }

            return oDirectRelationships.ToArray();
        }

        protected static IEnumerable<Relationship> Connect(IEnumerable<Relationship> relationships, string entity, string relationship)
        {
            List<Relationship> result = new List<Relationship>();

            List<Relationship> decreasing = new List<Relationship>(relationships);

            Relationship first = decreasing.FirstOrDefault(r => r.Entity == entity);
            if (first == null)
            {
                first = decreasing.FirstOrDefault(r => r.RelatedEntity == entity);
                if (first == null) throw new SchemaException(string.Format(SchemaMessages.RelationshipsNonConnected, relationship));
                decreasing.Remove(first);
                first = first.Reverse();
            }
            else
            {
                decreasing.Remove(first);
            }
            result.Add(first);

            Connect(first, decreasing, result);

            if (result.Count < relationships.Count()) throw new SchemaException(string.Format(SchemaMessages.RelationshipsNonConnected, relationship));

            return result;
        }

        private static void Connect(Relationship prev, List<Relationship> decreasing, List<Relationship> result)
        {
            Relationship relationship = decreasing.FirstOrDefault(r => r.Entity == prev.RelatedEntity);
            if (relationship == null)
            {
                relationship = decreasing.FirstOrDefault(r => r.RelatedEntity == prev.RelatedEntity);
                if (relationship == null) return;

                decreasing.Remove(relationship);
                relationship = relationship.Reverse();
            }
            else
            {
                decreasing.Remove(relationship);
            }
            result.Add(relationship);
            Connect(relationship, decreasing, result);
        }

        // Specialty-Department,Department(CollegeId)-College(Id, UniversityId)-University(Id)
        protected static string[] Split(string value)
        {
            string val = value.Trim();
            Dictionary<string, string> dict = new Dictionary<string, string>();
            int leftIndex = val.IndexOf('(');
            while (leftIndex != -1)
            {
                int rightIndex = val.IndexOf(')');
                if (rightIndex == -1) throw new SchemaException(string.Format(SchemaMessages.UnpairedParenthesis, val));
                string guid = Guid.NewGuid().ToString("B");
                string inner = val.Substring(leftIndex + 1, rightIndex - leftIndex - 1);
                dict.Add(guid, inner.Trim());
                val = val.Substring(0, leftIndex) + guid + val.Substring(rightIndex + 1);

                leftIndex = val.IndexOf('(');
            }

            string[] relationships = val.Split(',');
            for (int i = 0; i < relationships.Length; i++)
            {
                string s = relationships[i].Trim();
                foreach (KeyValuePair<string, string> p in dict)
                {
                    s = s.Replace(p.Key, string.Format("({0})", p.Value));
                }
                relationships[i] = s;
            }

            return relationships;
        }

        // Specialty(DepartmentId)-Department(Id, CollegeId)-College(Id, UniversityId)-University(Id)
        protected static T Create<T>(string value) where T : Relationship
        {
            Relationship relationship;
            if (typeof(T) == typeof(ManyToOneRelationship))
            {
                IEnumerable<ManyToOneDirectRelationship> rels = GenerateDirectRelationships<ManyToOneDirectRelationship>(value);
                relationship = new ManyToOneRelationship(rels);
            }
            else if (typeof(T) == typeof(OneToManyRelationship))
            {
                IEnumerable<OneToManyDirectRelationship> rels = GenerateDirectRelationships<OneToManyDirectRelationship>(value);
                relationship = new OneToManyRelationship(rels);
            }
            else if (typeof(T) == typeof(OneToOneRelationship))
            {
                IEnumerable<OneToOneDirectRelationship> rels = GenerateDirectRelationships<OneToOneDirectRelationship>(value);
                relationship = new OneToOneRelationship(rels);
            }
            else
            {
                IEnumerable<DirectRelationship> rels = GenerateDirectRelationships<DirectRelationship>(value);
                relationship = new Relationship(rels);
            }
            return relationship as T;
        }

        private static IEnumerable<T> GenerateDirectRelationships<T>(string value) where T : DirectRelationship
        {
            List<T> rels = new List<T>();

            KeyValuePair<string, string[]>[] segments = GetSegments(value);
            string entity = segments[0].Key;
            string[] properties = segments[0].Value;

            for (int i = 0; i < segments.Length - 1; i++)
            {
                string relatedEntity = segments[i + 1].Key;
                string[] nextProps = segments[i + 1].Value;

                string[] relatedProperties = new string[properties.Length];
                Array.Copy(nextProps, relatedProperties, properties.Length);
                string[] remain = new string[nextProps.Length - properties.Length];
                Array.Copy(nextProps, properties.Length, remain, 0, remain.Length);

                DirectRelationship rel;
                if (typeof(T) == typeof(ManyToOneDirectRelationship))
                {
                    rel = new ManyToOneDirectRelationship(entity, relatedEntity, properties, relatedProperties);
                }
                else if (typeof(T) == typeof(OneToManyDirectRelationship))
                {
                    rel = new OneToManyDirectRelationship(entity, relatedEntity, properties, relatedProperties);
                }
                else if (typeof(T) == typeof(OneToOneDirectRelationship))
                {
                    rel = new OneToOneDirectRelationship(entity, relatedEntity, properties, relatedProperties);
                }
                else
                {
                    rel = new DirectRelationship(entity, relatedEntity, properties, relatedProperties);
                }
                rels.Add(rel as T);

                entity = relatedEntity;
                properties = remain;
            }

            return rels;
        }

        private static KeyValuePair<string, string[]>[] GetSegments(string value)
        {
            List<KeyValuePair<string, string[]>> list = new List<KeyValuePair<string, string[]>>();
            string[] ss = value.Split('-');
            foreach (string val in ss)
            {
                int leftIndex = val.IndexOf('(');
                int rightIndex = val.IndexOf(')');
                string entity = val.Substring(0, leftIndex);
                string[] properties = val.Substring(leftIndex + 1, rightIndex - leftIndex - 1).Split(',');
                for (int i = 0; i < properties.Length; i++)
                {
                    properties[i] = properties[i].Trim();
                }
                list.Add(new KeyValuePair<string, string[]>(entity, properties));
            }
            return list.ToArray();
        }

        public Relationship Reverse()
        {
            if (this is ManyToManyRelationship) return Reverse(this as ManyToManyRelationship);

            if (this is PlainRelationship) return Reverse(this as PlainRelationship);

            return new Relationship(this.RelatedEntity, this.Entity, this.DirectRelationships.Reverse());
        }

        private static ManyToManyRelationship Reverse(ManyToManyRelationship relationship)
        {
            return new ManyToManyRelationship(
                (OneToManyRelationship)relationship.ManyToOneRelationship.Reverse(),
                (ManyToOneRelationship)relationship.OneToManyRelationship.Reverse());
        }

        private static PlainRelationship Reverse(PlainRelationship relationship)
        {
            string entity = relationship.RelatedEntity;
            string relatedEntity = relationship.Entity;
            List<DirectRelationship> relationships = new List<DirectRelationship>();
            for (int i = relationship.DirectRelationships.Length - 1; i >= 0; i--)
            {
                relationships.Add(relationship.DirectRelationships[i].Reverse());
            }

            if (relationship is ManyToOneRelationship)
            {
                return new OneToManyRelationship(entity, relatedEntity, relationships);
            }
            else if (relationship is OneToManyRelationship)
            {
                return new ManyToOneRelationship(entity, relatedEntity, relationships);
            }
            else if (relationship is OneToOneRelationship)
            {
                return new OneToOneRelationship(entity, relatedEntity, relationships);
            }
            else
            {
                throw new NotSupportedException(relationship.GetType().ToString()); // never
            }
        }

        // ManyToManyRelationship or PlainRelationship
        public XElement ToXml()
        {
            if (this is ManyToManyRelationship)
            {
                ManyToManyRelationship relationship = this as ManyToManyRelationship;

                XElement relationshipSchema = new XElement(SchemaVocab.Relationship);
                relationshipSchema.SetAttributeValue(SchemaVocab.Name, relationship.Name);

                relationshipSchema.SetAttributeValue(SchemaVocab.Type, SchemaVocab.ManyToMany);
                relationshipSchema.SetAttributeValue(SchemaVocab.Entity, relationship.Entity);
                relationshipSchema.SetAttributeValue(SchemaVocab.RelatedEntity, relationship.RelatedEntity);

                relationshipSchema.Add(PlainRelationshipToXml(relationship.OneToManyRelationship).Elements(SchemaVocab.Relationship));
                relationshipSchema.Add(PlainRelationshipToXml(relationship.ManyToOneRelationship).Elements(SchemaVocab.Relationship));

                return relationshipSchema;
            }

            return PlainRelationshipToXml(this as PlainRelationship);
        }

        private static XElement PlainRelationshipToXml(PlainRelationship relationship)
        {
            XElement relationshipSchema = new XElement(SchemaVocab.Relationship);
            relationshipSchema.SetAttributeValue(SchemaVocab.Name, relationship.Name);
            string type;
            if (relationship is ManyToOneRelationship)
            {
                type = SchemaVocab.ManyToOne;
            }
            else if (relationship is OneToManyRelationship)
            {
                type = SchemaVocab.OneToMany;
            }
            else if (relationship is OneToOneRelationship)
            {
                type = SchemaVocab.OneToOne;
            }
            else
            {
                throw new NotSupportedException(relationship.GetType().ToString()); // never
            }
            relationshipSchema.SetAttributeValue(SchemaVocab.Type, type);
            relationshipSchema.SetAttributeValue(SchemaVocab.Entity, relationship.Entity);
            relationshipSchema.SetAttributeValue(SchemaVocab.RelatedEntity, relationship.RelatedEntity);
            if (relationship.DirectRelationships.Length == 1)
            {
                DirectRelationship rel = relationship.DirectRelationships[0];
                for (int i = 0; i < rel.Properties.Length; i++)
                {
                    XElement xProperty = new XElement(SchemaVocab.Property);
                    xProperty.SetAttributeValue(SchemaVocab.Name, rel.Properties[i]);
                    xProperty.SetAttributeValue(SchemaVocab.RelatedProperty, rel.RelatedProperties[i]);
                    relationshipSchema.Add(xProperty);
                }
            }
            else
            {
                foreach (DirectRelationship rel in relationship.DirectRelationships)
                {
                    XElement relSchema = new XElement(SchemaVocab.Relationship);
                    string relType;
                    if (rel is ManyToOneDirectRelationship)
                    {
                        relType = SchemaVocab.ManyToOne;
                    }
                    else if (rel is OneToManyDirectRelationship)
                    {
                        relType = SchemaVocab.OneToMany;
                    }
                    else if (rel is OneToOneDirectRelationship)
                    {
                        relType = SchemaVocab.OneToOne;
                    }
                    else
                    {
                        throw new NotSupportedException(rel.GetType().ToString()); // never
                    }
                    relSchema.SetAttributeValue(SchemaVocab.Type, relType);
                    relSchema.SetAttributeValue(SchemaVocab.Entity, rel.Entity);
                    relSchema.SetAttributeValue(SchemaVocab.RelatedEntity, rel.RelatedEntity);
                    for (int i = 0; i < rel.Properties.Length; i++)
                    {
                        XElement xProperty = new XElement(SchemaVocab.Property);
                        xProperty.SetAttributeValue(SchemaVocab.Name, rel.Properties[i]);
                        xProperty.SetAttributeValue(SchemaVocab.RelatedProperty, rel.RelatedProperties[i]);
                        relSchema.Add(xProperty);
                    }
                    relationshipSchema.Add(relSchema);
                }
            }
            return relationshipSchema;
        }

        public override string ToString()
        {
            return string.Join(",", DirectRelationships.Select(r => r.ToString()));
        }

        public string ToCompactString()
        {
            string result = string.Format("{0}({1})-", DirectRelationships[0].Entity, string.Join(",", DirectRelationships[0].Properties));
            for (int i = 0; i < DirectRelationships.Length - 1; i++)
            {
                List<string> list = new List<string>();
                list.AddRange(DirectRelationships[i].RelatedProperties);
                list.AddRange(DirectRelationships[i + 1].Properties);
                result += string.Format("{0}({1})-", DirectRelationships[i].RelatedEntity, string.Join(",", list));
            }
            result += string.Format("{0}({1})", DirectRelationships[DirectRelationships.Length - 1].RelatedEntity,
                string.Join(",", DirectRelationships[DirectRelationships.Length - 1].RelatedProperties));
            return result;
        }

        // ManyToManyRelationship or PlainRelationship
        public static Relationship Create(XElement relationshipSchema)
        {
            Relationship relationship;
            string type = relationshipSchema.Attribute(SchemaVocab.Type).Value;
            if (type == SchemaVocab.ManyToMany)
            {
                relationship = CreateManyToManyRelationship(relationshipSchema);
            }
            else
            {
                relationship = CreatePlainRelationship(relationshipSchema);
            }

            relationship.Check();

            return relationship;
        }

        private static ManyToManyRelationship CreateManyToManyRelationship(XElement relationshipSchema)
        {
            string entity = relationshipSchema.Attribute(SchemaVocab.Entity).Value;
            string relatedEntity = relationshipSchema.Attribute(SchemaVocab.RelatedEntity).Value;

            XElement oneToManySchema = new XElement(SchemaVocab.Relationship);
            oneToManySchema.SetAttributeValue(SchemaVocab.Type, SchemaVocab.OneToMany);
            oneToManySchema.SetAttributeValue(SchemaVocab.Entity, entity);
            oneToManySchema.SetAttributeValue(SchemaVocab.RelatedEntity, string.Empty);
            oneToManySchema.Add(relationshipSchema.Elements(SchemaVocab.Relationship).Where(x => x.Attribute(SchemaVocab.Type).Value == SchemaVocab.OneToMany));
            OneToManyRelationship oneToManyRelationship = (OneToManyRelationship)CreatePlainRelationship(oneToManySchema);
            oneToManySchema.SetAttributeValue(SchemaVocab.RelatedEntity,
                oneToManyRelationship.DirectRelationships[oneToManyRelationship.DirectRelationships.Length - 1].RelatedEntity);

            XElement manyToOneSchema = new XElement(SchemaVocab.Relationship);
            manyToOneSchema.SetAttributeValue(SchemaVocab.Type, SchemaVocab.ManyToOne);
            manyToOneSchema.SetAttributeValue(SchemaVocab.Entity, string.Empty);
            manyToOneSchema.SetAttributeValue(SchemaVocab.RelatedEntity, relatedEntity);
            manyToOneSchema.Add(relationshipSchema.Elements(SchemaVocab.Relationship).Where(x => x.Attribute(SchemaVocab.Type).Value == SchemaVocab.ManyToOne));
            ManyToOneRelationship manyToOneRelationship = (ManyToOneRelationship)CreatePlainRelationship(manyToOneSchema);
            manyToOneSchema.SetAttributeValue(SchemaVocab.Entity, manyToOneRelationship.DirectRelationships[0].Entity);

            ManyToManyRelationship relationship = new ManyToManyRelationship(oneToManyRelationship, manyToOneRelationship);

            XAttribute attr = relationshipSchema.Attribute(SchemaVocab.Name);
            if (attr != null) relationship.Name = attr.Value;

            return relationship;
        }

        private static PlainRelationship CreatePlainRelationship(XElement relationshipSchema)
        {
            PlainRelationship relationship;
            if (relationshipSchema.Elements(SchemaVocab.Relationship).Any())
            {
                string type = relationshipSchema.Attribute(SchemaVocab.Type).Value;
                string entity = relationshipSchema.Attribute(SchemaVocab.Entity).Value;
                string relatedEntity = relationshipSchema.Attribute(SchemaVocab.RelatedEntity).Value;

                List<DirectRelationship> relList = new List<DirectRelationship>();
                foreach (XElement xDirectRelationship in relationshipSchema.Elements(SchemaVocab.Relationship))
                {
                    DirectRelationship rel = DirectRelationship.Create(xDirectRelationship, type);
                    relList.Add(rel);
                }
                IEnumerable<DirectRelationship> rels = DirectRelationship.Connect(relList);

                switch (type)
                {
                    case SchemaVocab.ManyToOne:
                        relationship = new ManyToOneRelationship(entity, relatedEntity, rels);
                        break;
                    case SchemaVocab.OneToMany:
                        relationship = new OneToManyRelationship(entity, relatedEntity, rels);
                        break;
                    case SchemaVocab.OneToOne:
                        relationship = new OneToOneRelationship(entity, relatedEntity, rels);
                        break;
                    default:
                        throw new NotSupportedException(type);
                }
            }
            else
            {
                DirectRelationship rel = DirectRelationship.Create(relationshipSchema);
                List<DirectRelationship> relList = new List<DirectRelationship>() { rel };
                if (rel is ManyToOneDirectRelationship)
                {
                    relationship = new ManyToOneRelationship(rel.Entity, rel.RelatedEntity, relList);
                }
                else if (rel is OneToManyDirectRelationship)
                {
                    relationship = new OneToManyRelationship(rel.Entity, rel.RelatedEntity, relList);
                }
                else if (rel is OneToOneDirectRelationship)
                {
                    relationship = new OneToOneRelationship(rel.Entity, rel.RelatedEntity, relList);
                }
                else
                {
                    throw new NotSupportedException(rel.GetType().ToString()); // never
                }
            }

            XAttribute attr = relationshipSchema.Attribute(SchemaVocab.Name);
            if (attr != null) relationship.Name = attr.Value;

            return relationship;
        }

        protected void Check()
        {
            string entity;
            string relatedEntity;
            if (this is ManyToManyRelationship)
            {
                ManyToManyRelationship manyToManyRelationship = this as ManyToManyRelationship;
                entity = manyToManyRelationship.OneToManyRelationship.Entity;
                relatedEntity = manyToManyRelationship.ManyToOneRelationship.RelatedEntity;
                if (manyToManyRelationship.OneToManyRelationship.RelatedEntity != manyToManyRelationship.ManyToOneRelationship.Entity)
                    throw new SchemaException(SchemaMessages.RelationshipsNonConnected);
            }
            else if (this is PlainRelationship)
            {
                PlainRelationship plainRelationship = this as PlainRelationship;
                entity = plainRelationship.DirectRelationships[0].Entity;
                relatedEntity = plainRelationship.DirectRelationships[plainRelationship.DirectRelationships.Length - 1].RelatedEntity;
            }
            else //if (this is Relationship)
            {
                entity = this.DirectRelationships[0].Entity;
                relatedEntity = this.DirectRelationships[this.DirectRelationships.Length - 1].RelatedEntity;
            }
            if (Entity != entity) throw new SchemaException(string.Format(SchemaMessages.ConflictingEntities, Entity, entity));
            if (RelatedEntity != relatedEntity) throw new SchemaException(string.Format(SchemaMessages.ConflictingRelatedEntities, RelatedEntity, relatedEntity));
        }


    }
}
