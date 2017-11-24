//Author: Sergey Lavrinenko
//Date:   28Nov2010

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;

namespace RequestEngine
{
    enum OperatorType
    {
        Not,
        And,
        Or,
        Xor,
        GreaterEqual,
        Greater,
        LessEqual,
        Less,
        Equal,
        NotEqual,
        Add,
        Substruct,
        Mult,
        Div
    }

    enum RecommendedInputTypes
    {
        Any,
        Similar,
        Comparable,
        Numeric,
        Boolean
    }

    /// <summary>
    /// Basic class for all Tokens.
    /// </summary>
    abstract class Token
    {
        public int Position { get; set; }

        public static Token CreateToken(string token, int position)
        {
            token = token.ToLower();

            switch (token)
            {
                case ".": return new PointToken() { Position = position };
                case ",": return new CommaToken() { Position = position };
                case "not": return new OperatorToken(OperatorType.Not);
                case "and": return new OperatorToken(OperatorType.And);
                case "or": return new OperatorToken(OperatorType.Or);
                case "xor": return new OperatorToken(OperatorType.Xor);
                case ">=": return new OperatorToken(OperatorType.GreaterEqual);
                case ">": return new OperatorToken(OperatorType.Greater);
                case "<=": return new OperatorToken(OperatorType.LessEqual);
                case "<": return new OperatorToken(OperatorType.Less);
                case "=": return new OperatorToken(OperatorType.Equal);
                case "<>": return new OperatorToken(OperatorType.NotEqual);
                case "+": return new OperatorToken(OperatorType.Add);
                case "-": return new OperatorToken(OperatorType.Substruct);
                case "*": return new OperatorToken(OperatorType.Mult);
                case "/": return new OperatorToken(OperatorType.Div);
                default: return null;
            }
        }
    }

    /// <summary>
    /// Simple Token representing "," in Function parameters and type cast expressions.
    /// </summary>
    class CommaToken : Token
    {
    }

    /// <summary>
    /// Simple Token representing "." in Property or Function call expressions.
    /// </summary>
    class PointToken : Token
    {
    }

    /// <summary>
    /// Privitive operator Tokens. Operation types: arithmetic, comparison, logical.
    /// Important is Priority property representing operators priority in expression. Currently configured same os in C#.
    /// Another important property is RecommendedInputTypes - based on it some type casts are done.
    /// Also acts like factory for building corresponding Linq Expressions.
    /// </summary>
    class OperatorToken : Token
    {
        OperatorType operatorType;

        public OperatorToken(OperatorType operatorType) { this.operatorType = operatorType; }

        public OperatorType OperatorType { get { return operatorType; } }

        /// <summary>
        /// Priority of current operator in expression
        /// </summary>
        public int Priority
        {
            get
            {
                switch (OperatorType)
                {
                    case OperatorType.Not: return 40;
                    case OperatorType.And: return 20;
                    case OperatorType.Or:
                    case OperatorType.Xor: return 10;
                    case OperatorType.Add:
                    case OperatorType.Substruct: return 30;
                    case OperatorType.Mult:
                    case OperatorType.Div: return 35;
                    default: return 25;
                }
            }
        }

        /// <summary>
        /// Is operator binary (or unary)
        /// </summary>
        public bool IsBinary
        {
            get
            {
                return operatorType != OperatorType.Not;
            }
        }

        /// <summary>
        /// Recommended input types for expression: bool for logical, numeric for arithmetic and comparable to comparison
        /// </summary>
        public RecommendedInputTypes RecommendedInputTypes
        {
            get
            {
                switch (operatorType)
                {
                    case OperatorType.Not:
                    case OperatorType.And:
                    case OperatorType.Or:
                    case OperatorType.Xor: return RecommendedInputTypes.Boolean;
                    case OperatorType.Add:
                    case OperatorType.Substruct:
                    case OperatorType.Mult:
                    case OperatorType.Div: return RecommendedInputTypes.Numeric;
                    case OperatorType.Greater:
                    case OperatorType.GreaterEqual:
                    case OperatorType.Less:
                    case OperatorType.LessEqual: return RecommendedInputTypes.Comparable;
                    case OperatorType.Equal:
                    case OperatorType.NotEqual: return RecommendedInputTypes.Any;
                    default: return RecommendedInputTypes.Any;
                }
            }
        }

        /// <summary>
        /// Creates corresponding binary Linq Expression with passed arguments
        /// </summary>
        public Expression CreateBinaryExpression(Expression argLeft, Expression argRight)
        {
            if (!IsBinary) throw new InvalidOperationException("CreateBinaryExpression called for unary operation");

            switch (operatorType)
            {
                case OperatorType.And: return Expression.AndAlso(argLeft, argRight);
                case OperatorType.Or: return Expression.OrElse(argLeft, argRight);
                case OperatorType.Xor: return Expression.ExclusiveOr(argLeft, argRight);
                case OperatorType.Greater: return Expression.GreaterThan(argLeft, argRight);
                case OperatorType.GreaterEqual: return Expression.GreaterThanOrEqual(argLeft, argRight);
                case OperatorType.Less: return Expression.LessThan(argLeft, argRight);
                case OperatorType.LessEqual: return Expression.LessThanOrEqual(argLeft, argRight);
                case OperatorType.Equal: return Expression.Equal(argLeft, argRight);
                case OperatorType.NotEqual: return Expression.NotEqual(argLeft, argRight);
                case OperatorType.Add: return Expression.Add(argLeft, argRight);
                case OperatorType.Substruct: return Expression.Subtract(argLeft, argRight);
                case OperatorType.Mult: return Expression.Multiply(argLeft, argRight);
                case OperatorType.Div: return Expression.Divide(argLeft, argRight);

                default: throw new NotImplementedException(operatorType.ToString() + " operation is not implemented");
            }
        }

        /// <summary>
        /// Creates corresponding unary Linq Expression with passed argument
        /// </summary>
        public Expression CreateUnaryExpression(Expression arg)
        {
            if (IsBinary) throw new InvalidOperationException("CreateUnaryExpression called for binary operation");

            switch (operatorType)
            {
                case OperatorType.Not: return Expression.Not(arg);
                default: throw new NotImplementedException(operatorType.ToString() + " operation is not implemented");
            }
        }
    }

    /// <summary>
    /// This is special composite Token which can contain many inner Tokens (all which were in backets).
    /// </summary>
    class BracketsToken : Token
    {
        private List<Token> tokens;

        public List<Token> Tokens { get { return tokens; } }

        public BracketsToken(List<Token> tokens)
        {
            this.tokens = tokens;
        }
    }

    /// <summary>
    /// This Token basically represents all sequences of characters which weren't classified as previous types of Tokens.
    /// Practical meaning is all constants, Properties and Function calls (Conversion operators are also treated as function calls).
    /// Serves as a Factory for conversion experessions
    /// </summary>
    class MemberOrConstantToken : Token
    {
        string strValue;
        bool isString;

        public MemberOrConstantToken(string strVal, bool isString)
        {
            this.strValue = strVal;
            this.isString = isString;
        }

        /// <summary>
        /// String value of the token
        /// </summary>
        public string StrValue
        {
            get { return strValue; }
        }

        /// <summary>
        /// True if this is a string constant (was inside "" in original expression)
        /// </summary>
        public bool IsString
        {
            get { return isString; }
        }

        /// <summary>
        /// True if this is conversion operator (currently defined for primitive C# types and DateTime)
        /// </summary>
        public bool IsConversion
        {
            get
            {
                string lower = strValue.ToLower();
                switch (lower)
                {
                    case "bool":
                    case "byte":
                    case "char":
                    case "ushort":
                    case "short":
                    case "int":
                    case "uint":
                    case "long":
                    case "ulong":
                    case "float":
                    case "double":
                    case "decimal":
                    case "string":
                    case "DateTime": return true;
                    default: return false;
                }
            }
        }

        /// <summary>
        /// Creates conversion expression
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        public Expression CreateConversion(Expression argument)
        {
            string lower = strValue.ToLower();
            switch (lower)
            {
                case "bool": return Expression.Convert(argument, typeof(bool));
                case "byte": return Expression.Convert(argument, typeof(byte));
                case "char": return Expression.Convert(argument, typeof(char));
                case "ushort": return Expression.Convert(argument, typeof(ushort));
                case "short": return Expression.Convert(argument, typeof(short));
                case "int": return Expression.Convert(argument, typeof(int));
                case "uint": return Expression.Convert(argument, typeof(uint));
                case "long": return Expression.Convert(argument, typeof(long));
                case "ulong": return Expression.Convert(argument, typeof(ulong));
                case "float": return Expression.Convert(argument, typeof(float));
                case "double": return Expression.Convert(argument, typeof(double));
                case "decimal": return Expression.Convert(argument, typeof(decimal));
                case "string": return Expression.Convert(argument, typeof(string));
                case "DateTime": return Expression.Convert(argument, typeof(DateTime));
                default: throw new BuilderException(Position, BuilderException.BuilderExceptionType.UnrecognizedConversionType, lower);
            }
        }
    }
}
