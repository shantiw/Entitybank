using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Schema
{
    public sealed class ManyToManyRelationship : Relationship
    {
        public OneToManyRelationship OneToManyRelationship { get; private set; }
        public ManyToOneRelationship ManyToOneRelationship { get; private set; }

        public ManyToManyRelationship(OneToManyRelationship oneToManyRelationship, ManyToOneRelationship manyToOneRelationship)
            : base(oneToManyRelationship.Entity, manyToOneRelationship.RelatedEntity, GetDirectRelationships(oneToManyRelationship, manyToOneRelationship))
        {
            OneToManyRelationship = oneToManyRelationship;
            ManyToOneRelationship = manyToOneRelationship;
        }

        private static IEnumerable<DirectRelationship> GetDirectRelationships(OneToManyRelationship oneToManyRelationship, ManyToOneRelationship manyToOneRelationship)
        {
            List<DirectRelationship> list = new List<DirectRelationship>(oneToManyRelationship.DirectRelationships);
            list.AddRange(manyToOneRelationship.DirectRelationships);
            return list;
        }


    }
}
