using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler
{
    class SARtemp : SARbase
    {
        public SARbase[] storedData = new SARbase[3];
        public SARtemp(string _xId, SARbase left, SARoperator op, SARbase right)
        {
            xId = _xId;
            symbol = (Symbol)left.symbol.Clone();
            symbol.symid = xId;
            storedData[0] = left;
            storedData[1] = op;
            storedData[2] = right;
            // check that the operation is valid
            validOp();
        }
        private void validOp()
        {
            switch (storedData[1].token.lexeme)
            {
                case "+":
                    if (!storedData[0].symbol.data[0][1].Equals(storedData[2].symbol.data[0][1]))
                    {
                        genError(storedData[1].token.lineNum, storedData[2].symbol.data[0][1], storedData[0].symbol.data[0][1]);
                    }
                    break;
                case "-":
                    if (!storedData[0].symbol.data[0][1].Equals(storedData[2].symbol.data[0][1]))
                    {
                        genError(storedData[1].token.lineNum, storedData[2].symbol.data[0][1], storedData[0].symbol.data[0][1]);
                    }
                    break;
                case "*":
                    if (!storedData[0].symbol.data[0][1].Equals(storedData[2].symbol.data[0][1]))
                    {
                        genError(storedData[1].token.lineNum, storedData[2].symbol.data[0][1], storedData[0].symbol.data[0][1]);
                    }
                    break;
                case "/":
                    if (!storedData[0].symbol.data[0][1].Equals(storedData[2].symbol.data[0][1]))
                    {
                        genError(storedData[1].token.lineNum, storedData[2].symbol.data[0][1], storedData[0].symbol.data[0][1]);
                    }
                    break;
                case "<":
                case ">":
                case "<=":
                case ">=":
                case "!=":
                case "==":
                    if (!storedData[0].symbol.data[0][1].Equals(storedData[2].symbol.data[0][1]))
                    {
                        if (storedData[2].symbol.data[0][1] != "null")
                        {
                            genError(storedData[1].token.lineNum, storedData[2].symbol.data[0][1], storedData[0].symbol.data[0][1]);
                        }
                    }
                    symbol.data[0][1] = "bool";
                    break;
                case "and":
                case "or":
                    if (!storedData[0].symbol.data[0][1].Equals(storedData[2].symbol.data[0][1]))
                    {
                        genError(storedData[1].token.lineNum, storedData[2].symbol.data[0][1], storedData[0].symbol.data[0][1]);
                    }
                    symbol.data[0][1] = "bool";
                    break;
                default:
                    genError(storedData[1].token.lineNum, storedData[1].token.lexeme, "valid operator");
                    break;
            }

        }
        public void genError(int curLine, string found, string expectation)
        {
            Console.WriteLine(curLine + ": Found " + found + " expecting " + expectation);
            System.Environment.Exit(-1);
        }
    }


}
