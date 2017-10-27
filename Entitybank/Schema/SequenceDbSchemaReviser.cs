using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    public class SequenceDbSchemaReviser : IDbSchemaReviser
    {
        // ".*{0}.*{1}.*", TableName, ColumnName
        protected string Format { get; private set; }

        public SequenceDbSchemaReviser(string format)
        {
            Format = format;
        }

        public virtual XElement Revise(XElement dbSchema)
        {
            XElement schema = new XElement(dbSchema);

            foreach (XElement xTable in schema.Elements(SchemaVocab.Table))
            {
                if (Format.Contains("{1}"))
                {
                    foreach (XElement xColumn in xTable.Elements(SchemaVocab.Column))
                    {
                        string sequenceName = string.Format(Format, xTable.Attribute(SchemaVocab.Name).Value, xColumn.Attribute(SchemaVocab.Name).Value);
                        XElement xSequence = schema.Elements(SchemaVocab.Sequence).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == sequenceName);
                        if (xSequence != null)
                        {
                            xColumn.SetAttributeValue(SchemaVocab.Sequence, sequenceName);
                        }
                    }
                }
                else
                {
                    string sequenceName = string.Format(Format, xTable.Attribute(SchemaVocab.Name).Value);
                    XElement xSequence = schema.Elements(SchemaVocab.Sequence).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == sequenceName);
                    if (xSequence != null)
                    {
                        if (xTable.Attribute(SchemaVocab.PrimaryKey) != null)
                        {
                            string[] primaryKey = xTable.Attribute(SchemaVocab.PrimaryKey).Value.Split(new char[] { ',' });
                            if (primaryKey.Length == 1)
                            {
                                XElement xColumn = xTable.Elements(SchemaVocab.Column).First(x => x.Attribute(SchemaVocab.Name).Value == primaryKey[0]);
                                xColumn.SetAttributeValue(SchemaVocab.Sequence, sequenceName);
                            }
                        }
                    }
                }
            }
            return schema;
        }


    }
}
