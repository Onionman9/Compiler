using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler
{
    public enum TokenType
    {
        NUMBER, CHARACTER, CLASS_DECLARATION, IDENTIFIER, PUNCTUATION, KEYWORDS, MATH_OPERATORS, RELATIONAL_OPERATORS, ARRAY_BEGIN, ARRAY_END, BLOCK_BEGIN, BLOCK_END, PARENTHESES_OPEN, PARENTHESES_CLOSE, IO_OPERATORS, UNKNOWN, EOF, ASSIGNMENT_OPERATOR
    }
    ///   <summary>
    ///    The Token Class represents the possible tokens that we can have
    ///    </summary>
    public class Token
    {
        public string lexeme;
        public TokenType thisType;
        public int lineNum;

        public Token(string _lexeme, TokenType _thisType, int _lineNum) 
        {
            lexeme = _lexeme;
            thisType = _thisType;
            lineNum = _lineNum;
        }

        public override string ToString() 
        {
            string returnVal = "";
            returnVal += lineNum + " " + lexeme + " ";
            switch(thisType)
            {
                case TokenType.ASSIGNMENT_OPERATOR:
                    returnVal += "ASSIGNMENT_OPERATOR";
                    break;
                case TokenType.NUMBER:
                    returnVal += "NUMBER";
                    break;
                case TokenType.CLASS_DECLARATION:
                    returnVal += "CLASS_DECLARATION";
                    break;
                case TokenType.CHARACTER:
                    returnVal += "CHARACTER";
                    break;
                case TokenType.IDENTIFIER:
                    returnVal += "IDENTIFIER";
                    break;
                case TokenType.PUNCTUATION:
                    returnVal += "PUNCTUATION";
                    break;
                case TokenType.KEYWORDS:
                    returnVal += "KEYWORDS";
                    break;
                case TokenType.MATH_OPERATORS:
                    returnVal += "MATH_OPERATORS";
                    break;
                case TokenType.RELATIONAL_OPERATORS:
                    returnVal += "RELATIONAL_OPERATORS";
                    break;
                case TokenType.ARRAY_BEGIN:
                    returnVal += "ARRAY_BEGIN";
                    break;
                case TokenType.ARRAY_END:
                    returnVal += "ARRAY_END";
                    break;
                case TokenType.BLOCK_BEGIN:
                    returnVal += "BLOCK_BEGIN";
                    break;
                case TokenType.BLOCK_END:
                    returnVal += "BLOCK_END";
                    break;
                case TokenType.PARENTHESES_OPEN:
                    returnVal += "PARENTHESES_OPEN";
                    break;
                case TokenType.PARENTHESES_CLOSE:
                    returnVal += "PARENTHESES_CLOSE";
                    break;
                case TokenType.IO_OPERATORS:
                    returnVal += "IO_OPERATORS";
                    break;
                case TokenType.UNKNOWN:
                    returnVal += "UNKNOWN";
                    break;
                case TokenType.EOF:
                    returnVal += "EOF";
                    break;
                default:
                    returnVal += "DEFAULT TYPE - ERROR";
                    break;
            }
            return returnVal;
        }
    }
}