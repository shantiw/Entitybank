using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Schema
{
    [Serializable]
    public class SchemaException : Exception
    {
        public SchemaException() : base()
        {
        }

        public SchemaException(string message) : base(message)
        {
        }

        public SchemaException(string message, Exception innerException) : base(message, innerException)
        {
        }


    }
}
