using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler
{
    class SAR_NEW : SARbase
    {
        public List<SARbase> args;
        public SAR_Type type;
        public SAR_NEW(string _inId, SAR_Type _type, SAR_AL _args)
        {
            xId = _inId;
            args = new List<SARbase>();
            args = _args.args;
            type = _type;
        }

        public bool Verify()
        {
            bool valid = true;
            return valid;
        }
    }
}
