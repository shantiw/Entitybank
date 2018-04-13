using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.DataObjects;
using XData.Data.Modification;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    // Database.Generic.Modification.Original.cs
    public partial class Database<T>
    {
        protected int Execute_Original(UpdateCommandNode<T> node, Modifier<T> modifier)
        {
            int affected = Execute(node as UpdateCommand<T>, modifier);
            if (node.ChildrenCollection.Count == 0) return affected;

            //
            Dictionary<string, object> dbPropertyValues = FetchFromDb(node.PropertyValues, node.EntitySchema, node.UniqueKeySchema).First();

            foreach (UpdateCommandNodeChildren<T> nodeChildren in node.ChildrenCollection)
            {
                string childrenPath = nodeChildren.Path;

                DirectRelationship relationship = nodeChildren.ParentRelationship;
                string childEntity = relationship.RelatedEntity;
                XElement childEntitySchema = node.Schema.GetEntitySchema(childEntity);
                XElement childKeySchema = SchemaHelper.GetKeySchema(childEntitySchema);

                Dictionary<string, object> relatedPropertyValues = GetRelatedPropertyValues(relationship, dbPropertyValues);

                //
                IEnumerable<IReadOnlyDictionary<string, object>> refetchedChildPVs = FetchRelatedCommands(relatedPropertyValues, relationship.RelatedEntity, node.Schema);
                IEnumerable<IReadOnlyDictionary<string, object>> childPVs = nodeChildren.UpdateCommandNodes.Select(n => n.PropertyValues);
                foreach (IReadOnlyDictionary<string, object> refetchedChildPV in refetchedChildPVs)
                {
                    Dictionary<string, object> refetchedKeyChildPV = new Dictionary<string, object>();
                    foreach (string property in childKeySchema.Elements(SchemaVocab.Property).Select(x => x.Attribute(SchemaVocab.Name).Value))
                    {
                        refetchedKeyChildPV.Add(property, refetchedChildPV[property]);
                    }

                    IReadOnlyDictionary<string, object> found = Find(childPVs, refetchedKeyChildPV);
                    if (found == null)
                    {
                        throw new ConstraintException(ErrorMessages.InsertedByAnotherUser);
                    }
                }

                //
                foreach (UpdateCommandNode<T> childNode in nodeChildren.UpdateCommandNodes)
                {
                    // establishing relationship
                    foreach (KeyValuePair<string, object> propertyValue in relatedPropertyValues)
                    {
                        if (!childNode.FixedUpdatePropertyValues.ContainsKey(propertyValue.Key))
                        {
                            childNode.FixedUpdatePropertyValues.Add(propertyValue.Key, propertyValue.Value);
                        }
                    }

                    //
                    Execute_Original(childNode, modifier);
                }
            }

            return affected;
        }


    }
}
