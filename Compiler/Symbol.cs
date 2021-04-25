using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Compiler
{
    [Serializable]
    public class Symbol
    {
        public string symid; // this is also the symid
        public string scope;
        public string lexeme;
        public string kind;
        public List<string>[] data =  new List<string>[3];
        // Additional Info needed
        public int byteSize = 0;
        public int curOffset = 0;
        public Symbol(string _scope, string _symid, string _lexeme, string _kind)
        {
            data[0] = new List<string>();
            data[1] = new List<string>();
            data[2] = new List<string>();
            symid = _symid;
            scope = _scope;
            lexeme = _lexeme;
            kind = _kind;
        }

        public override string ToString() 
        {
            string retString = "\n";
            retString += "Scope: \t" + scope + '\n';
            retString += "Symid: \t" + symid + '\n';
            retString += "Lexeme: " + lexeme + '\n';
            retString += "Kind: \t" + kind + '\n';
            retString += "Data: \t";
            bool firstFlag = true;
            foreach (List<string> sList in data) 
            {
                if (!firstFlag)
                {
                    retString += "\t";
                }
                firstFlag = false;
                foreach (string s in sList) 
                {
                    retString += s;
                    retString += " ";
                }
                retString += "\n";
            };
            retString += "Byte Size: " + byteSize + "\n";
            retString += "Offset: " + curOffset + "\n";
            return retString;
        
        }

        public object Clone()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, this);
            ms.Position = 0;
            object copy = bf.Deserialize(ms);
            ms.Close();
            return copy;
        }
    }
}
