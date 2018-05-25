using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Objects
{
    public enum Execution { Create, Delete, Update }

    public class ExecutionEntry<T>
    {
        public Execution Execution { get; private set; }
        public T Aggreg { get; private set; }
        public string Entity { get; private set; }
        public XElement Schema { get; private set; }

        public ExecutionEntry(Execution execution, T aggreg, string entity, XElement schema)
        {
            Execution = execution;
            Aggreg = aggreg;
            Entity = entity;
            Schema = schema;
        }
    }

    public class ValidatingEventArgs<T> : EventArgs
    {
        public Execution Execution { get; private set; }
        public T Aggreg { get; private set; }
        public string Entity { get; private set; }
        public XElement Schema { get; private set; }
        public IEnumerable<ExecutionEntry<T>> Context { get; private set; }

        public ICollection<ValidationResult> ValidationResults { get; private set; }

        public ValidatingEventArgs(Execution execution, T aggreg, string entity, XElement schema, IEnumerable<ExecutionEntry<T>> context)
        {
            Execution = execution;
            Aggreg = aggreg;
            Entity = entity;
            Schema = schema;
            Context = context;

            ValidationResults = new List<ValidationResult>();
        }

    }

    public delegate void ValidatingEventHandler<T>(object sender, ValidatingEventArgs<T> args);

}
