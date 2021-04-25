using System;
using System.Collections.Generic;
using System.Text;
/*
    Abstract semenatic action record parent class
 */
namespace Compiler
{
    abstract public class SARbase
    {
        public Symbol symbol;
        public Token token;
        public string xId;
    }
}
