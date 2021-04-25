using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler
{
    public class SARliteral : SARbase
    {
        public SARliteral(string _xId,Symbol _symbol)
        {
            symbol = _symbol;
            xId = _xId;
        }

    }
}
