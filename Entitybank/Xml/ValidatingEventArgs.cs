using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Objects;

namespace XData.Data.Xml
{
    public class ExecutionEntry
    {
        public Execution Execution { get; private set; }
        public XElement Element { get; private set; }
        public string Entity { get; private set; }
        public XElement Schema { get; private set; }

        public ExecutionEntry(Execution execution, XElement element, string entity, XElement schema)
        {
            Execution = execution;
            Element = element;
            Entity = entity;
            Schema = schema;
        }
    }

    public class ValidatingEventArgs : EventArgs
    {
        public Execution Execution { get; private set; }
        public XElement Element { get; private set; }
        public string Entity { get; private set; }
        public XElement Schema { get; private set; }
        public IEnumerable<ExecutionEntry<XElement>> Context { get; private set; }

        public ICollection<ValidationResult> ValidationResults { get; private set; }

        public ValidatingEventArgs(Execution execution, XElement element, string entity, XElement schema, IEnumerable<ExecutionEntry<XElement>> context)
        {
            Execution = execution;
            Element = element;
            Entity = entity;
            Schema = schema;
            Context = context;

            ValidationResults = new List<ValidationResult>();
        }

    }

    public delegate void ValidatingEventHandler(object sender, ValidatingEventArgs args);

}
