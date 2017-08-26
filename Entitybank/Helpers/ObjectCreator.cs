using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Helpers
{
    internal class ObjectCreator
    {
        public XElement Config { get; private set; }
        protected Type Type;
        protected Func<object> CreateFunc;
        protected Func<object[], object> CreateFuncWithParams;
        protected object[] Arguments;

        public ObjectCreator(XElement config)
        {
            Config = config;

            //
            string sType = Config.Attribute("type").Value;
            Type = TypeHelper.GetType(sType);

            //
            if (Config.HasElements)
            {
                List<Type> types = new List<Type>();
                List<object> objs = new List<object>();
                foreach (XElement argument in Config.Elements())
                {
                    string sArgType = argument.Attribute("type").Value;
                    Type type = Type.GetType(sArgType);
                    types.Add(type);
                    string value = argument.Attribute("value").Value;
                    object obj = Convert.ChangeType(value, type);
                    objs.Add(obj);
                }
                CreateFuncWithParams = Create2Func(Type, types.ToArray());
                Arguments = objs.ToArray();
            }
            else
            {
                CreateFunc = Create2Func(Type);
            }
        }

        public object CreateInstance()
        {
            if (CreateFunc == null)
            {
                return CreateFuncWithParams(Arguments);
            }
            else
            {
                return CreateFunc();
            }
        }

        private static Func<object> Create2Func(Type type)
        {
            NewExpression newExpr = Expression.New(type);
            Expression<Func<object>> lambdaExpr = Expression.Lambda<Func<object>>(newExpr, null);
            return lambdaExpr.Compile();
        }

        private static Func<object[], object> Create2Func(Type type, Type[] paramTypes)
        {
            ConstructorInfo constructor = type.GetConstructor(paramTypes);

            ParameterExpression paramExpr = Expression.Parameter(typeof(object[]), "_args");
            Expression[] arguments = GetArguments(paramTypes, paramExpr);

            NewExpression newExpr = Expression.New(constructor, arguments);

            Expression<Func<object[], object>> lambdaExpr = Expression.Lambda<Func<object[], object>>(newExpr, paramExpr);
            return lambdaExpr.Compile();
        }

        private static Expression[] GetArguments(Type[] types, ParameterExpression paramExpr)
        {
            List<Expression> exprList = new List<Expression>();
            for (int i = 0; i < types.Length; i++)
            {
                var paramObj = Expression.ArrayIndex(paramExpr, Expression.Constant(i));
                var exprObj = Expression.Convert(paramObj, types[i]);
                exprList.Add(exprObj);
            }
            return exprList.ToArray();
        }


    }
}
