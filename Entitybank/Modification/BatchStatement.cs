using System.Collections.Generic;

namespace XData.Data.Modification
{
    public class BatchStatement
    {
        public string Sql { get; private set; }
        public IReadOnlyDictionary<string, object> Parameters { get; private set; }

        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }

        public BatchStatement(string sql, IReadOnlyDictionary<string, object> parameters, int startIndex, int endIndex)
        {
            Sql = sql;
            Parameters = parameters;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }


    }
}
