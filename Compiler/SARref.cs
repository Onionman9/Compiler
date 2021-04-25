using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler
{
    class SARref : SARbase
    {

        public SARbase[] storedData = new SARbase[2];
        public SARref(string _xId, SARbase left, SARbase right)
        {
            xId = _xId;
            symbol = (Symbol)right.symbol.Clone(); // our type should reflect the most specific member reference so sampleCat.x should have x's type not sampleCats 
            storedData[0] = left;
            storedData[1] = right;
        }
        public void genError(int curLine, string found, string expectation)
        {
            Console.WriteLine(curLine + ": Found " + found + " expecting " + expectation);
            System.Environment.Exit(-1);
        }
    }
}
