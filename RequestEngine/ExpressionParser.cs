//Author: Sergey Lavrinenko
//Date:   28Nov2010

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RequestEngine
{
    /// <summary>
    /// This class provides mechanism for parsing string expression to group of tokens.
    /// </summary>
    static class ExpressionParser
    {
        /// <summary>
        /// This function parses given expression and returns resulted tokens. Can throw ParserException exception in case of not valid expression.
        /// </summary>
        internal static List<Token> ParseExpression(string expression)
        {
            List<Token> result = new List<Token>();
            List<int> brackets = new List<int>();
            StringBuilder strToken = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < expression.Length; ++i)
            {
                char currChar = expression[i];

                if (!inQuotes && currChar == '\"')
                    ProcessToken(strToken, brackets, result, i);

                if (ProcessQuates(currChar, ref inQuotes, strToken, result, i)) continue;

                if (currChar == ' ')
                {
                    ProcessToken(strToken, brackets, result, i);
                    continue;
                }

                strToken.Append(currChar);

                if (i == expression.Length - 1 || IsDelimeter(strToken.ToString(), expression[i + 1]))
                    ProcessToken(strToken, brackets, result, i);
            }

            if (inQuotes) throw new ParserException(expression.Length, ParserException.ParserExceptionType.QuatesNotClosed);
            if (brackets.Count != 0) throw new ParserException(expression.Length, ParserException.ParserExceptionType.BracketsNotClosed);

            return result;
        }

        /// <summary>
        /// This function processes quates (") in string expression. Mostly used for giving user a possibility to indicate string constant in expression.
        /// </summary>
        /// <param name="currChar">Current character we are parsing</param>
        /// <param name="inQuates">True if we already in quates</param>
        /// <param name="strToken">String accumulated for token</param>
        /// <param name="result">True if processing of character was done and no futture processing is required</param>
        /// <param name="index">Index of current character we're parsing</param>
        /// <returns></returns>
        static bool ProcessQuates(char currChar, ref bool inQuates, StringBuilder strToken, List<Token> result, int index)
        {
            if (currChar == '"')
            {
                if (inQuates)
                {
                    result.Add(new MemberOrConstantToken(strToken.ToString(), true) { Position = index });
                    strToken.Remove(0, strToken.Length);
                }
                inQuates = !inQuates;
                return true;
            }
            if (inQuates)
            {
                strToken.Append(currChar);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Function checking if current character or accumulated expression is delimeter. Need to check token expression as we have delimeters >1 charecters long
        /// </summary>
        /// <param name="strToken">String representing sum of characters currently accumulated for token</param>
        /// <param name="nextChar">Next character in parsed string</param>
        /// <returns>True if current token expression or next character are delimeters</returns>
        static bool IsDelimeter(string strToken, char nextChar)
        {
            switch (strToken)
            {
                case ".":
                case ",":
                case "+":
                case "-":
                case "*":
                case "/":
                case "=":
                case "<=":
                case ">=":
                case "<>":
                case "(":
                case ")": return true;
                case "<": return nextChar != '=' && nextChar != '>';
                case ">": return nextChar != '=';
            }
            switch (nextChar)
            {
                case '.':
                case ',':
                case '+':
                case '-':
                case '*':
                case '/':
                case '=':
                case '<':
                case '>':
                case '(':
                case ')': return true;
            }
            return false;
        }

        /// <summary>
        /// Processing passed token expression. This fucntion actually creates token from its string representation and adds to tokens list. 
        /// Takes brackets into account as well.
        /// </summary>
        /// <param name="strToken">String representation of the token</param>
        /// <param name="brackets">List of brackets' indexes. Only () brackets are handled</param>
        /// <param name="result">Tokens list</param>
        /// <param name="index">Index of current character we're parsing</param>
        static void ProcessToken(StringBuilder strToken, List<int> brackets, List<Token> result, int index)
        {
            if (strToken.Length == 0) return;

            string tempToken = strToken.ToString();

            Token token = Token.CreateToken(tempToken, index);
            if (token != null)
                result.Add(token);
            else if (tempToken == "(")
            {
                brackets.Add(result.Count);
                strToken.Remove(0, strToken.Length);
                return;
            }
            else if (tempToken == ")")
            {
                if (brackets.Count == 0)
                    throw new ParserException(index, ParserException.ParserExceptionType.InvalidBracketsOrder);

                int startIndex = brackets[brackets.Count - 1];
                brackets.RemoveAt(brackets.Count - 1);

                List<Token> bracketTokens = new List<Token>();
                for (int i = startIndex; i <= result.Count - 1; ++i)
                    bracketTokens.Add(result[i]);

                if (result.Count - 1 - startIndex >= 0)
                {
                    result.RemoveRange(startIndex + 1, result.Count - 1 - startIndex);
                    result[startIndex] = new BracketsToken(bracketTokens) { Position = index };
                }
                else
                    result.Add(new BracketsToken(bracketTokens) { Position = index });
            }
            else
                result.Add(new MemberOrConstantToken(strToken.ToString(), false) { Position = index });

            strToken.Remove(0, strToken.Length);
        }
    }
}
