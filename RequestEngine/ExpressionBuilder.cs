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
    /// <summary>
    /// This class builds Functors based on input strings: Func<T, Result>, Func<T1, T2, Result>. It is easy to extent to any required # of Args.
    /// Basically almost any arithmetical and logical expression which can bew written in C# is supported including properties and function calls.
    /// Not supported: ?:, ??
    /// Priorities for operations is the same as in C#.
    /// Arguments passed to Func during execution can be accessed using arg0, arg1, ... keywords, but arg0 may not be indicated - used by default.
    /// During construction it is checked that resulted type returned by expresion equal to expected type and if not ExpressionTypeException is thrown.
    /// In case of incorrect expression (incorrect tokens order, keywords misusage etc) BuilderException exception thrown from any function.
    /// 
    /// Approximate syntax:
    /// const               =       any valid constant expression, strings can be taken in ""
    /// argument            =       arg0, arg1, ... argN                                                                                //case incensitive    
    /// bin_operator        =       +|-|*|/|=|<|<=|<>|>|>=|and|or|xor                                                                   //case incensitive
    /// un_operator         =       not                                                                                                 //case incensitive
    /// type_convertor      =       bool, byte, char, short, ushort, int, uint, long, ulong, float, double, decimal, string, DateTime   //case incensitive
    /// type_conversion     =       type_convertor(expression)
    /// bin_operation       =       expression bin_operator expression
    /// un_operation        =       un_operator expression
    /// property            =       [arg0].PropName | argument.PropName | (expression).PropName
    /// arg_list            =       expression, expression, ... , expression
    /// function_call       =       expression.FuncName(arg_list)
    /// expression          =       const | argument | property | function_call | bin_operation | un_operation
    /// </summary>
    public static class ExpressionBuilder
    {
        /// <summary>
        /// Constructs Func<T, Result> functor based on stringExpression passed
        /// </summary>
        public static Func<T, TResult> BuildFunctor<T, TResult>(string stringExpression)
        {
            if (stringExpression == null || stringExpression == string.Empty) return null;

            List<Token> tokens = ExpressionParser.ParseExpression(stringExpression);
            if (tokens.Count == 0) return null;

            ParameterExpression context = Expression.Parameter(typeof(T), "context");
            List<Expression> contexts = new List<Expression>();
            contexts.Add(context);
            Expression expression = BuildExpression(new ListSegment<Token>(tokens), contexts, typeof(TResult));

            Expression<Func<T, TResult>> lambda =
                Expression.Lambda<Func<T, TResult>>(expression, new ParameterExpression[] { context });
            Func<T, TResult> functor = lambda.Compile();

            return functor;
        }

        /// <summary>
        /// Constructs Func<T1, T2, Result> functor based on stringExpression passed
        /// </summary>
        public static Func<T1, T2, TResult> BuildFunctor<T1, T2, TResult>(string stringExpression)
        {
            if (stringExpression == null || stringExpression == string.Empty) return null;

            List<Token> tokens = ExpressionParser.ParseExpression(stringExpression);
            if (tokens.Count == 0) return null;

            ParameterExpression context1 = Expression.Parameter(typeof(T1), "context1");
            ParameterExpression context2 = Expression.Parameter(typeof(T2), "context2");
            List<Expression> contexts = new List<Expression>();
            contexts.Add(context1);
            contexts.Add(context2);
            Expression expression = BuildExpression(new ListSegment<Token>(tokens), contexts, typeof(TResult));

            Expression<Func<T1, T2, TResult>> lambda =
                Expression.Lambda<Func<T1, T2, TResult>>(expression, new ParameterExpression[] { context1, context2 });
            Func<T1, T2, TResult> functor = lambda.Compile();

            return functor;
        }

        /// <summary>
        /// Builds expression based on provided list of Tokens. Uses contexts to access parameters that will be passed externally.
        /// </summary>
        /// <param name="tokens">Tokens list</param>
        /// <param name="contexts">List of ParameterExpressions - variables which will be passed as Funtor's arguments</param>
        /// <param name="recommendedType">If not null, tryes to convert resulted type of expression to recommendedType (no 100% guarantee)</param>
        /// <returns>Expression</returns>
        private static Expression BuildExpression(ListSegment<Token> tokens, List<Expression> contexts, Type recommendedType)
        {
            if (tokens == null || tokens.Count == 0) return null;

            Expression result = null;
            int operatorTokenIndex = FindLowestPriorityOperatorTokenIndex(tokens);

            if (operatorTokenIndex != -1)
            {
                OperatorToken operatorToken = tokens[operatorTokenIndex] as OperatorToken;
                if (operatorToken.IsBinary)
                {
                    if (operatorTokenIndex == 0)
                        throw new BuilderException(operatorToken.Position, BuilderException.BuilderExceptionType.NoLeftOperand);
                    if (operatorTokenIndex == tokens.Count - 1)
                        throw new BuilderException(operatorToken.Position, BuilderException.BuilderExceptionType.NoRightOperand);

                    ListSegment<Token> leftArgTokens = tokens.GetSegment(0, operatorTokenIndex);
                    ListSegment<Token> rightArgTokens = tokens.GetSegment(operatorTokenIndex + 1, tokens.Count - operatorTokenIndex - 1);

                    Expression argLeft = null;
                    Expression argRight = null;

                    switch (operatorToken.RecommendedInputTypes)
                    {
                        case RecommendedInputTypes.Boolean:
                            TryAssignBinaryOpArgs(leftArgTokens, rightArgTokens, contexts, typeof(bool), out argLeft, out argRight);
                            break;
                        case RecommendedInputTypes.Comparable:
                        case RecommendedInputTypes.Numeric:
                            if (TryAssignBinaryOpArgs(leftArgTokens, rightArgTokens, contexts, null, out argLeft, out argRight)) break;
                            if (TryAssignBinaryOpArgs(rightArgTokens, leftArgTokens, contexts, null, out argLeft, out argRight))
                            {
                                ListSegment<Token> temp = leftArgTokens;
                                leftArgTokens = rightArgTokens;
                                rightArgTokens = temp;
                                break;
                            }
                            if (TryAssignBinaryOpArgs(leftArgTokens, rightArgTokens, contexts, typeof(int), out argLeft, out argRight)) break;
                            if (TryAssignBinaryOpArgs(leftArgTokens, rightArgTokens, contexts, typeof(float), out argLeft, out argRight)) break;
                            if (TryAssignBinaryOpArgs(leftArgTokens, rightArgTokens, contexts, typeof(bool), out argLeft, out argRight)) break;
                            if (TryAssignBinaryOpArgs(leftArgTokens, rightArgTokens, contexts, typeof(DateTime), out argLeft, out argRight)) break;
                            if (TryAssignBinaryOpArgs(leftArgTokens, rightArgTokens, contexts, typeof(string), out argLeft, out argRight)) break;
                            break;
                        default:
                            if (TryAssignBinaryOpArgs(leftArgTokens, rightArgTokens, contexts, null, out argLeft, out argRight)) break;
                            break;
                    }

                    result = operatorToken.CreateBinaryExpression(argLeft, argRight);
                }
                else
                {
                    if (operatorTokenIndex != 0 || tokens.Count != 2)
                        throw new BuilderException(operatorToken.Position, BuilderException.BuilderExceptionType.IncorrectUnaryOperatorPosition);

                    Expression arg = BuildExpression(tokens.GetSegment(1, 1), contexts, null);
                    result = operatorToken.CreateUnaryExpression(arg);
                }
            }
            else
                result = BuildMembersExpression(tokens, contexts, recommendedType);

            if (recommendedType != null && result.Type != recommendedType)
                throw new ExpressionTypeException(operatorTokenIndex == -1 ? tokens[0].Position : tokens[operatorTokenIndex].Position, recommendedType, result.Type);
            return result;
        }

        /// <summary>
        /// Builds expressions related to Property extraction, Function call, Type conversion and Constants
        /// </summary>
        /// <param name="tokens">Tokens list</param>
        /// <param name="contexts">List of ParameterExpressions - variables which will be passed as Funtor's arguments</param>
        /// <param name="recommendedType">If not null, tryes to convert resulted type of expression to recommendedType (no 100% guarantee)</param>
        /// <returns>Expression</returns>
        private static Expression BuildMembersExpression(ListSegment<Token> tokens, List<Expression> contexts, Type recommendedType)
        {
            int state = 0; //0-separator, 1-expression, 2-function
            Expression result = contexts[0];
            MemberOrConstantToken methodToken = null;
            MemberOrConstantToken conversionToken = null;

            for (int i = 0; i < tokens.Count; ++i)
            {
                switch (state)
                {
                    case 0:
                        {
                            MemberOrConstantToken mcToken = tokens[i] as MemberOrConstantToken;
                            if (mcToken != null)
                            {
                                if (mcToken.IsString)
                                {
                                    if (i == 0) result = Expression.Constant(mcToken.StrValue);
                                    else throw new BuilderException(mcToken.Position, BuilderException.BuilderExceptionType.UnexpectedConstant, mcToken.StrValue);
                                }
                                else if (i == tokens.Count - 1 || tokens[i + 1] is PointToken)
                                {
                                    if (i == 0 && mcToken.StrValue.Length > 2 && mcToken.StrValue.Substring(0, 3).ToLower() == "arg")
                                    {
                                        string numstr = mcToken.StrValue.Substring(3);
                                        if (numstr != string.Empty)
                                        {
                                            int num = 0;
                                            try { num = Convert.ToInt32(numstr); }
                                            catch { throw new BuilderException(mcToken.Position, BuilderException.BuilderExceptionType.WrongArgFormat); }
                                            if (num >= contexts.Count) throw new BuilderException(mcToken.Position, BuilderException.BuilderExceptionType.ArgNumberExceedsMax);
                                            result = contexts[num];
                                        }
                                    }
                                    else
                                    {
                                        PropertyInfo pi = TypeUtils.GetPropertyInfo(result.Type, mcToken.StrValue);
                                        if (pi != null) result = Expression.Property(result, pi);
                                        else if (i == 0) result = CreateConstantExpression(mcToken, tokens.Count == 1 ? recommendedType : null);
                                        else throw new BuilderException(mcToken.Position, BuilderException.BuilderExceptionType.PropertyNotExists, pi.Name);
                                    }
                                }
                                else
                                {
                                    if (i == 0 && mcToken.IsConversion)
                                    {
                                        conversionToken = mcToken;
                                        state = 2;
                                    }
                                    else
                                    {
                                        if (TypeUtils.MethodExists(result.Type, mcToken.StrValue)) { state = 2; methodToken = mcToken; }
                                        else if (i == 0) result = CreateConstantExpression(mcToken, tokens.Count == 1 ? recommendedType : null);
                                        else throw new BuilderException(mcToken.Position, BuilderException.BuilderExceptionType.FunctionNotExists, mcToken.StrValue);
                                    }
                                }
                                if (state == 0) state = 1;
                                break;
                            }
                            
                            BracketsToken brToken = tokens[i] as BracketsToken;
                            if (brToken != null)
                            {
                                result = BuildExpression(new ListSegment<Token>(brToken.Tokens), contexts, null);
                                state = 1;
                                break;
                            }

                            throw new BuilderException(tokens[i].Position, BuilderException.BuilderExceptionType.IncorrectExpression);
                        }
                    case 1:
                        if (tokens[i] is PointToken) state = 0;
                        else throw new BuilderException(tokens[i].Position, BuilderException.BuilderExceptionType.UnexpectedExpression);
                        break;
                    case 2:
                        {
                            BracketsToken brToken = tokens[i] as BracketsToken;
                            if (brToken != null)
                            {
                                int argsCount = GetParamsNumber(brToken);

                                if (methodToken != null)
                                {
                                    MethodInfo mi = TypeUtils.GetMethodInfo(result.Type, methodToken.StrValue, 0);
                                    if (mi == null) throw new BuilderException(methodToken.Position, BuilderException.BuilderExceptionType.NoFunctionFound);
                                    if (argsCount == 0)
                                        result = Expression.Call(result, mi);
                                    else
                                    {
                                        ParameterInfo[] pinfos = mi.GetParameters();
                                        List<Type> recommendedTypes = new List<Type>();

                                        for (int j = 0; j < pinfos.Length; ++j)
                                            if (!pinfos[j].IsOut && !pinfos[j].IsRetval) recommendedTypes.Add(pinfos[j].ParameterType);
                                            else throw new BuilderException(tokens[i].Position, BuilderException.BuilderExceptionType.ParameterTypeNotSupported, pinfos[j].Name);
                                        
                                        List<Expression> args = BuildFunctionArgumentsList(brToken, contexts, recommendedTypes);                                  
                                        result = Expression.Call(result, mi, args);
                                    }
                                }
                                else if (conversionToken != null)
                                {
                                    if (argsCount != 1) throw new BuilderException(conversionToken.Position, BuilderException.BuilderExceptionType.WrongArgumentsNumber, "1 expected");
                                    
                                    List<Type> recommendedTypes = new List<Type>();
                                    recommendedTypes.Add(null);

                                    List<Expression> args = BuildFunctionArgumentsList(brToken, contexts, recommendedTypes);
                                    result = conversionToken.CreateConversion(args[0]);
                                }
                                else
                                    throw new BuilderException(brToken.Position, BuilderException.BuilderExceptionType.UnexpectedError);
                                
                                methodToken = null;
                                conversionToken = null;
                                state = 1;
                            }
                            else
                                throw new BuilderException(tokens[i].Position, BuilderException.BuilderExceptionType.FunctionArgumentsExpected);
                        }
                        break;
                }
            }

            if (state == 2)
                throw new BuilderException(tokens[tokens.Count - 1].Position, BuilderException.BuilderExceptionType.FunctionArgumentsExpected);

            return result;
        }

        /// <summary>
        /// Creates constant expression. Main problem is to pick up right type if no recommended provided.
        /// </summary>
        private static Expression CreateConstantExpression(MemberOrConstantToken mcToken, Type recommendedType)
        {
            object obj;
            if (recommendedType != null)
                TypeUtils.ConvertFromString(mcToken.StrValue, recommendedType, out obj);
            else
                do
                {
                    if (TypeUtils.ConvertFromString(mcToken.StrValue, typeof(int), out obj)) break;
                    if (TypeUtils.ConvertFromString(mcToken.StrValue, typeof(float), out obj)) break;
                    if (TypeUtils.ConvertFromString(mcToken.StrValue, typeof(bool), out obj)) break;
                    if (TypeUtils.ConvertFromString(mcToken.StrValue, typeof(DateTime), out obj)) break;
                    if (TypeUtils.ConvertFromString(mcToken.StrValue, typeof(string), out obj)) break;
                }
                while (false);

            Expression result = recommendedType == null ? Expression.Constant(obj) : Expression.Constant(obj, recommendedType);
            return result;
        }

        /// <summary>
        /// Builds list of arguments for function call. Checks for arg list expression correctness. 
        /// Main problems: try to convert argument types to types expected by function and check that number of actual arguments correspond to expected
        /// </summary>
        /// <param name="brackets">This is token, which contains all tokens related to arg list</param>
        /// <param name="contexts">List of ParameterExpressions - variables which will be passed as Funtor's arguments</param>
        /// <param name="recommendedTypes">List of recommended types of arguments</param>
        /// <returns>List of Expressions which will be used as arguments for function calls</returns>
        private static List<Expression> BuildFunctionArgumentsList(BracketsToken brackets, List<Expression> contexts, List<Type> recommendedTypes)
        {
            int startIndex = 0;
            int currIndex = 0;
            List<Expression> arguments = new List<Expression>();
            if (brackets.Tokens.Count == 0) return arguments;

            for (; currIndex < brackets.Tokens.Count; ++currIndex)
            {
                Token currToken = brackets.Tokens[currIndex];
                if (currToken is CommaToken)
                {
                    if (startIndex > currIndex - 1)
                        throw new BuilderException(currToken.Position, BuilderException.BuilderExceptionType.FunctionArgumentsExpected);

                    arguments.Add(BuildExpression(new ListSegment<Token>(brackets.Tokens, startIndex, currIndex - startIndex), contexts, recommendedTypes[arguments.Count]));
                    startIndex = currIndex + 1;
                }
            }

            if (startIndex > currIndex - 1)
                throw new BuilderException(brackets.Tokens[currIndex].Position, BuilderException.BuilderExceptionType.FunctionArgumentsExpected);
            arguments.Add(BuildExpression(new ListSegment<Token>(brackets.Tokens, startIndex, currIndex - startIndex), contexts, recommendedTypes[arguments.Count]));

            return arguments;
        }

        /// <summary>
        /// Simply finds index of lowest priority Operator Token among all tokens
        /// </summary>
        private static int FindLowestPriorityOperatorTokenIndex(ListSegment<Token> tokens)
        {
            OperatorToken operatorToken = null;
            int operatorTokenIndex = -1;
            for (int i = 0; i < tokens.Count; ++i)
            {
                OperatorToken opToken = tokens[i] as OperatorToken;
                if (opToken != null && (operatorToken == null || opToken.Priority < operatorToken.Priority))
                {
                    operatorTokenIndex = i;
                    operatorToken = opToken;
                }
            }
            return operatorTokenIndex;
        }

        private static int GetParamsNumber(BracketsToken brackets)
        {
            if (brackets.Tokens.Count == 0) return 0;

            int count=1;
            for (int i = 0; i < brackets.Tokens.Count; ++i)
                if (brackets.Tokens[i] is CommaToken) ++count;
            return count;
        }

        private static bool TryAssignBinaryOpArgs(ListSegment<Token> leftArgTokens, ListSegment<Token> rightArgTokens, List<Expression> contexts, Type recommendedType,
            out Expression argLeft, out Expression argRight)
        {
            argLeft = null;
            argRight = null;
            try
            {
                argLeft = BuildExpression(leftArgTokens, contexts, recommendedType);
                argRight = BuildExpression(rightArgTokens, contexts, argLeft.Type);
                return true;
            }
            catch (ExpressionTypeException) { return false; }
        }  
    }
}
