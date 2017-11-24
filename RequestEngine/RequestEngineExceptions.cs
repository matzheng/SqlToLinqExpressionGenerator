//Author: Sergey Lavrinenko
//Date:   28Nov2010

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RequestEngine
{
    /// <summary>
    /// Basic type of exception for this engine. 
    /// Important difference is Position property it provides: vaue is index of character in string expression on which problem was detected.
    /// </summary>
    public class RequestEngineException : ApplicationException
    {
        int position;

        public int Position
        {
            get { return position; }
        }

        internal RequestEngineException(int position, string message = "") : base(message) { this.position = position; }
    }

    /// <summary>
    /// All ExpressionParser's exceptions
    /// </summary>
    public class ParserException : RequestEngineException
    {
        public enum ParserExceptionType 
        { 
            QuatesNotClosed, 
            BracketsNotClosed, 
            InvalidBracketsOrder 
        }

        ParserExceptionType type;

        internal ParserException(int position, ParserExceptionType type, string message = "") : base(position, message) { this.type = type; }

        public ParserExceptionType Type { get { return type; } }
    }

    /// <summary>
    /// All ExpressionBuilder's exceptions
    /// </summary>
    public class BuilderException : RequestEngineException
    {
        public enum BuilderExceptionType
        {
            WrongArgFormat,
            ArgNumberExceedsMax,
            UnrecognizedConversionType,
            UnexpectedConstant,
            UnexpectedExpression,
            PropertyNotExists,
            FunctionNotExists,
            ParameterTypeNotSupported,
            FunctionArgumentsExpected,
            WrongArgumentsNumber,
            NoFunctionFound,
            NoLeftOperand,
            NoRightOperand,
            IncorrectUnaryOperatorPosition,
            IncorrectExpression,
            UnexpectedError
        }

        BuilderExceptionType type;

        public BuilderException(int position, BuilderExceptionType type, string message = "") : base(position, message) { this.type = type; }

        public BuilderExceptionType Type { get { return type; } }
    }

    /// <summary>
    /// All exceptions related to types incompatibilities found in expression
    /// </summary>
    public class ExpressionTypeException : RequestEngineException
    {
        Type expectedType;
        Type obtainedType;

        public Type ExpectedType
        {
            get { return expectedType; }
        }

        public Type ObtainedType
        {
            get { return obtainedType; }
        }

        public ExpressionTypeException(int position, Type expectedType, Type obtainedType) : this(position, expectedType, obtainedType, string.Empty) { }

        public ExpressionTypeException(int position, Type expectedType, Type obtainedType, string message)
            : base(position, message)
        {
            this.expectedType = expectedType;
            this.obtainedType = obtainedType;
        }
    }
}
