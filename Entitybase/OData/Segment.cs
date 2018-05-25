using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.OData
{
    // Filter
    public abstract class Segment
    {
        public string Value { get; private set; }

        public Segment(string value)
        {
            Value = value;
        }
    }

    public class StringSegment : Segment
    {
        public StringSegment(string value) : base(value)
        {
        }
    }

    public class NullSegment : Segment
    {
        public NullSegment() : base("null")
        {
        }
    }

    public class ConstantSegment : Segment
    {
        public object Constant { get; private set; }

        public ConstantSegment(string value) : base(value)
        {
            if (value.StartsWith("datetime'"))
            {
                Constant = DateTime.Parse(value.Substring(9, value.Length - 10));
            }
            else if (value.StartsWith("'") && value.EndsWith("'"))
            {
                Constant = value.Substring(1, value.Length - 2).Replace("''", "'");
            }
        }

        public ConstantSegment(string value, object constant) : base(value)
        {
            Constant = constant;
        }
    }

    public class TrueSegment : ConstantSegment
    {
        public TrueSegment() : base("true", true)
        {
        }
    }

    public class FalseSegment : ConstantSegment
    {
        public FalseSegment() : base("false", false)
        {
        }
    }

    public class OperatorSegment : Segment
    {
        public string Operator { get; private set; }

        public OperatorSegment(string value) : base(value)
        {
            Operator = value;
        }
    }

    public class PropertySegment : Segment
    {
        public string Property { get; private set; }

        public PropertySegment(string value) : base(value)
        {
            Property = value;
        }
    }

    public class ParameterSegment : Segment
    {
        public string Parameter { get; private set; }

        public ParameterSegment(string value) : base(value)
        {
            Parameter = value;
        }
    }

    public class ParenthesesPairSegment : Segment
    {
        public Segment Inner { get; private set; }

        public ParenthesesPairSegment(string value, Segment inner) : base(value)
        {
            Inner = inner;
        }
    }

    public abstract class FuncSegment : Segment
    {
        public string Func { get; private set; }

        public FuncSegment(string value, string func) : base(value)
        {
            Func = func;
        }
    }

    public class ParamlessFuncSegment : FuncSegment
    {
        public ParamlessFuncSegment(string value, string func) : base(value, func)
        {
        }
    }

    public class UnaryFuncSegment : FuncSegment
    {
        public Segment Operand { get; private set; }

        public UnaryFuncSegment(string value, string func, Segment operand) : base(value, func)
        {
            Operand = operand;
        }
    }

    public class BinaryFuncSegment : FuncSegment
    {
        public Segment Left { get; private set; }
        public Segment Right { get; private set; }

        public BinaryFuncSegment(string value, string func, Segment left, Segment right) : base(value, func)
        {
            Left = left;
            Right = right;
        }
    }

    public class ArraySegment : Segment
    {
        public Segment[] Segments { get; private set; }

        public ArraySegment(string value, IEnumerable<Segment> segments) : base(value)
        {
            Segments = segments.ToArray();
        }
    }


}
