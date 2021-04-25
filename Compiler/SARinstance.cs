using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler
{
    public class SARinstance : SARbase
    {
        public SARinstance(string _xId, Symbol _symbol) 
        {
            symbol = _symbol; 
            xId = _xId;
        }

    }
}
