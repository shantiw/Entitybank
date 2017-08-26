using System.Collections.Generic;

namespace XData.Data.DataObjects
{
    // Generator
    public class SelectClauseCollection
    {
        public string Select { get; internal set; }
        public string From { get; internal set; }
        public IEnumerable<string> LeftJoins { get; internal set; }
        public string Where { get; internal set; }
        public string OrderBy { get; internal set; }

        internal SelectClauseCollection()
        {
        }

        public SelectClauseCollection(string select, string from, IEnumerable<string> leftJoins, string where, string orderBy, params string[] clauses)
        {
            Select = select;
            From = from;
            LeftJoins = leftJoins ?? new string[0];
            Where = where;
            OrderBy = orderBy;
        }

    }

    public class PagingClauseCollection : SelectClauseCollection
    {
        public SelectClauseCollection SelectClauses { get; internal set; }
        public string[] Clauses { get; set; }

        public PagingClauseCollection(SelectClauseCollection selectClauses) : base()
        {
            Select = selectClauses.Select;
            From = selectClauses.From;
            LeftJoins = new List<string>(selectClauses.LeftJoins);
            Where = selectClauses.Where;
            OrderBy = selectClauses.OrderBy;
        }

        public PagingClauseCollection(SelectClauseCollection selectClauses, string select, string from, IEnumerable<string> leftJoins, string where, string orderBy, params string[] clauses)
            : base(select, from, leftJoins, where, orderBy)
        {
            SelectClauses = selectClauses;
            Clauses = clauses;
        }

    }

}
