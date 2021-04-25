using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler
{
    class SARArr : SARbase
    {
        public SARArr(string _xId, SARbase left, SARbase right)
        {
            xId = _xId;
            symbol = (Symbol)right.symbol.Clone();
        }
        public void genError(int curLine, string found, string expectation)
        {
            Console.WriteLine(curLine + ": Found " + found + " expecting " + expectation);
            System.Environment.Exit(-1);
        }
    }


}
