using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler
{
    class SAR_Func : SARbase
    {
        public SAR_AL funcArgs;
        public SAR_Func(string _xId, Symbol _sIn, SAR_AL _funcArgs)
        {
            xId = _xId;
            symbol = _sIn;
            funcArgs = _funcArgs;
        }

    }
}
