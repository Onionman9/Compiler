using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler
{
    class SAR_AL : SARbase
    {
        public List<SARbase> args;
        public SAR_AL(string _inId) 
        {
            xId = _inId;
            args = new List<SARbase>();
        }
    }
}
