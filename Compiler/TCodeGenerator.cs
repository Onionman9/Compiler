using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

/*
    This class serves to generator TCode from our completed ICode and SymbolTable
 */
namespace Compiler
{
    class TCodeGenerator
    {
        public Dictionary<string, Symbol> symbolHashSet;
        public QuadTable finalQuad;
        // Extra debugging
        private bool debug = true;
        public int internlLblCnt = 0;
        private int paramOffset = -12;
        public string nextLbl = "";
        public TCodeGenerator(Dictionary<string, Symbol> _symbolHashSet, QuadTable _finalQuad) 
        {
            symbolHashSet = _symbolHashSet;
            finalQuad = _finalQuad;
        }

        public void writeTCodeFile() 
        {
            StreamWriter writie = new StreamWriter("tcode.asm");
            // Write all the globals into the file, before we do anything else
            WriteGlobals(writie);
            // Write the Quad table as TCode
            WriteQuad(writie);

            writie.Close();
        }

        public int getLoc(string findSymId) 
        {
            if (findSymId == "this") 
            {
                return -8;
            }
            int location = symbolHashSet[findSymId].curOffset;

            // Globals - They will have labels and be at the top of the code there is no ffset
            if (symbolHashSet[findSymId].scope == "g" && symbolHashSet[findSymId].lexeme[0] != 'T' && symbolHashSet[findSymId].lexeme[0] != 'R')
            {
                return 0;
            }
            // instance Var returns the location on the activation record which should always be > 0 (likely near the memeory limit)
            else
            {
                return symbolHashSet[findSymId].curOffset;
            }
        
        }


        /*
            Write the globals out at the beginning of the heap
         */
        public void WriteGlobals(StreamWriter sr)
        {
            sr.WriteLine("// GLOBALS BELOW");
            foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
            {
                if (IsGlobal(s.Value))
                {
                    if (s.Value.symid == "true")
                    {
                        sr.WriteLine("true .INT 1");
                    }
                    else if (s.Value.symid == "false")
                    {
                        sr.WriteLine("false .INT 0");
                    }
                    else if (s.Value.symid == "null")
                    {
                        sr.WriteLine("null .INT 0");
                    }
                    else if (s.Value.kind == "ilit")
                    {
                        sr.WriteLine(s.Value.symid + " .INT " + s.Value.lexeme);
                    }
                    else if (s.Value.kind == "clit")
                    {
                        if (s.Value.lexeme == "'\\n'")
                        {
                            sr.WriteLine(s.Value.symid + " .BYT " + "'\\" + "10'");
                        } else if (s.Value.lexeme == "' '") 
                        {
                            sr.WriteLine(s.Value.symid + " .BYT " + "'\\" + "32'");
                        }
                        else if (IsCharNum(s.Value.lexeme))
                        {
                            int baseAscii = 48;

                            string tempie = s.Value.lexeme;
                            tempie = tempie.Replace("'", "");
                            baseAscii += Int32.Parse(tempie);

                            sr.WriteLine(s.Value.symid + " .BYT " + "'\\" + baseAscii + "'");
                        }
                        else
                        {
                            sr.WriteLine(s.Value.symid + " .BYT " + s.Value.lexeme);
                        }
                    }
                }
            }
        }

        /*
            Verifies that a character is a number or not, so that if we have a char int we write it correctly to the VM
         */
        private bool IsCharNum(string lexeme)
        {
            if (lexeme == "'0'" || lexeme == "'1'" || lexeme == "'2'" || lexeme == "'3'" || lexeme == "'4'" || lexeme == "'5'" || lexeme == "'6'" || lexeme == "'7'" || lexeme == "'8'" || lexeme == "'9'") 
            {
                return true;
            }
            return false;
        }

        /*
            Write the globals out at the beginning of the heap
         */
        public void WriteQuad(StreamWriter sr)
        {
            sr.WriteLine("// QUAD TABLE AS TARGET CODE BELOW");
            int counter = 0;
            string calledFunc = "";
            foreach (DataRow row in finalQuad.quad.Rows)
            {
                string[] returnStrArr = new string[6];

                returnStrArr[0] = (string)finalQuad.quad.Rows[counter][0];
                returnStrArr[1] = (string)finalQuad.quad.Rows[counter][1];
                returnStrArr[2] = (string)finalQuad.quad.Rows[counter][2];
                returnStrArr[3] = (string)finalQuad.quad.Rows[counter][3];
                returnStrArr[4] = (string)finalQuad.quad.Rows[counter][4];
                returnStrArr[5] = (string)finalQuad.quad.Rows[counter][5];
                counter++;
                string tCodeLine = "";
                // Add Labels 
                int arSize = 0;
                int offSet = 0;
                tCodeLine += returnStrArr[0];
                if (nextLbl != "") 
                {
                    tCodeLine += nextLbl;
                    nextLbl = "";
                }
                if (tCodeLine.Length != 0) 
                {
                    tCodeLine += " "; 
                }
                switch (returnStrArr[1]) 
                {
                    case "FRAME":
                        // Create activation record
                        tCodeLine += "MOV R5 SP // FRAME " + returnStrArr[2];
                        sr.WriteLine(tCodeLine);
                        sr.WriteLine("MOV R0 FP");
                        sr.WriteLine("ADI R5 #-4"); // This puts us at our PFP (return value should be below this on the AR stack) this should also point to our current AR location
                        sr.WriteLine("STR R0 (R5)");
                        sr.WriteLine("ADI R5 #-4"); // This puts us at our "this" we stor this address in R6 (return value should be below this on the AR stack) 
                        offSet = getLoc(returnStrArr[3]);
                        // get this 
                        if (returnStrArr[3] != "this" && IsGlobal(symbolHashSet[returnStrArr[3]]))
                        {
                            // Note this should only ever have returnStrArr[2] == null, as we can only have objects and null as a this value
                            if (returnStrArr[3] != "null")
                            {
                                throw new Exception("Invalid This");
                            }
                            sr.WriteLine("LDR R6 " + returnStrArr[2]);
                        }
                        else if (offSet >= 0 && !(IsGlobal(symbolHashSet[returnStrArr[3]])))
                        {
                            // Getting a value on HEAP
                            sr.WriteLine("LDR R6 FP");
                            sr.WriteLine("ADI R6 #-8");
                            sr.WriteLine("LDR R6 (R6)"); // LOCATION IN HEAP of current object
                            sr.WriteLine("ADI R6 #" + offSet);
                            sr.WriteLine("LDR R6 (R6)"); // LOCATION IN HEAP of instance var
                        }
                        else
                        {
                            sr.WriteLine("LDR R6 FP");
                            sr.WriteLine("ADI R6 #" + offSet);
                            sr.WriteLine("LDR R6 (R6)");
                        }
                        sr.WriteLine("STR R6 (R5) // END FRAME");
                        calledFunc = returnStrArr[2]; // This if for debugging
                        paramOffset = -12;
                        // TODO: IF WE ARE IN AN OBJECT WE MEED TO SET "THIS" ON OUR FUNCTION FP = return, FP -4 = PFP, FP - 8 = this 
                        // When we return set SP to top of PFP

                        break;
                    case "CALL":
                        tCodeLine += "MOV R5 PC // BEGIN METHOD CALL + " + calledFunc;
                        sr.WriteLine(tCodeLine);
                        sr.WriteLine("ADI R5 #20");
                        sr.WriteLine("MOV FP SP"); // Updating the FP to the new SP
                        sr.WriteLine("STR R5 (FP)"); // dont forget to increment SP by the size of the function
                        arSize = symbolHashSet[returnStrArr[2]].byteSize; // get the size of the activation record (stored in symbol table)
                        sr.WriteLine("ADI SP #" + arSize);
                        sr.WriteLine("JMP " + returnStrArr[2]);
                        sr.WriteLine("MOV R2 FP");
                        sr.WriteLine("MOV SP FP"); // move SP back to FP and then set the FP to the PFP
                        sr.WriteLine("ADI R2 #-4"); // R2 is now the address of our pfp
                        sr.WriteLine("LDR R2 (R2)");
                        sr.WriteLine("MOV FP R2 // END CALL"); // move the PFP value into our FP
                        calledFunc = ""; // reset this after we call the function
                        paramOffset = -12;
                        break;
                    case "EXIT":
                        tCodeLine += "TRP 0"; // set the PFP
                        sr.WriteLine(tCodeLine);
                        break;
                    case "FUNC":
                        try
                        {
                            arSize = symbolHashSet[returnStrArr[2]].byteSize;
                        }
                        catch (KeyNotFoundException e) 
                        {
                            arSize = 0;
                        }// get the size of the activation record (stored in symbol table)
                        tCodeLine += "ADI SP #" + arSize; // NOTE: ARSIZE SHOULD NEVER BE POSITIVE 
                        sr.WriteLine(tCodeLine);
                        break;
                    case "MOV":
                        offSet = getLoc(returnStrArr[2]);

                        if (offSet >= 0)
                        {
                            // HANDLE GETTING A REFERENCE LOCATION HERE() It'll be on the heap
                            tCodeLine += "MOV R4 FP";
                            sr.WriteLine(tCodeLine);
                            sr.WriteLine("ADI R4 #-8");
                            sr.WriteLine("LDR R4 (R4)"); // Loads the heap address
                            sr.WriteLine("ADI R4 #" + offSet); // get location in heap (we are storing to here)

                        }
                        else if (returnStrArr[2][0] == 'R') 
                        {
                            // handle an array here 
                            tCodeLine += "MOV R4 FP";
                            sr.WriteLine(tCodeLine);
                            sr.WriteLine("ADI R4 #" + offSet);
                            sr.WriteLine("LDR R4 (R4)"); // Loads the heap address
                        }
                        else
                        {
                            tCodeLine += "MOV R4 FP";
                            sr.WriteLine(tCodeLine); // tCodeLine will have the label preceding if there is a label;
                            sr.WriteLine("ADI R4 #" + offSet);
                        }

                        // Now find location of the right set
                        offSet = getLoc(returnStrArr[3]);
                        if (IsGlobal(symbolHashSet[returnStrArr[3]]))
                        {
                            // just move the global in R0 and then store
                            if (symbolHashSet[returnStrArr[3]].data[0][1] != "char")
                            {
                                sr.WriteLine("LDR R0 " + returnStrArr[3]);
                                sr.WriteLine("STR R0 (R4)");
                            }
                            else
                            {
                                sr.WriteLine("LDB R0 " + returnStrArr[3]);
                                sr.WriteLine("STB R0 (R4)");
                            }
                        }
                        else
                        {

                            // for values on the AR, get the value by location and store it in the left side value (NOTE: The only postive offsets are REF variables which are calculated in REF commands and return an temp var that is on the AR)
                            sr.WriteLine("MOV R0 FP");
                            sr.WriteLine("ADI R0 #" + offSet);
                            sr.WriteLine("LDR R0 (R0)"); // We get the temp var value of our heap address here
                            sr.WriteLine("STR R0 (R4)");
                        }
                        break;
                    case "WRITEI":
                        offSet = getLoc(returnStrArr[2]);
                        if (IsGlobal(symbolHashSet[returnStrArr[2]]))
                        {

                            tCodeLine += "LDR R3 " + returnStrArr[2];
                            sr.WriteLine(tCodeLine);
                            sr.WriteLine("TRP 1");
                        }
                        else if (offSet >= 0)
                        {
                            // We are here if we are writing a referance variable in a class
                            tCodeLine += "MOV R3 FP";
                            sr.WriteLine(tCodeLine);
                            sr.WriteLine("ADI R3 #-8");
                            sr.WriteLine("LDR R3 (R3)"); // Loads the heap address
                            sr.WriteLine("ADI R3 #" + offSet); // get location in heap
                            sr.WriteLine("LDR R3 (R3)"); // Loads the value at the heap location

                            sr.WriteLine("TRP 1");
                        }
                        else
                        {
                            if (returnStrArr[2][0] == 'R' && (symbolHashSet[returnStrArr[2]].lexeme.Contains('[') && symbolHashSet[returnStrArr[2]].lexeme.Contains(']')))
                            {
                                // HANDLE ARRAYS
                                tCodeLine += "MOV R3 FP";
                                sr.WriteLine(tCodeLine);
                                sr.WriteLine("ADI R3 #" + offSet); // temp var on AR
                                sr.WriteLine("LDR R3 (R3)"); // LOCATION IN HEAP
                                sr.WriteLine("LDR R3 (R3)"); // value in heap
                                // if it nested load one more time (note: consider loop for handling more than one instance of nesting)
                                sr.WriteLine("TRP 1");
                            }
                            else 
                            {                                
                                // HANDLE REFS
                                tCodeLine += "MOV R3 FP";
                                sr.WriteLine(tCodeLine);
                                sr.WriteLine("ADI R3 #" + offSet);
                                sr.WriteLine("LDR R3 (R3) // assumed ref var");
                                if (symbolHashSet[returnStrArr[2]].data[0][1] != "int" && symbolHashSet[returnStrArr[2]].data[0][1] != "char" && symbolHashSet[returnStrArr[2]].data[0][1] != "bool") 
                                {
                                    sr.WriteLine("LDR R3 (R3)");
                                }
                                sr.WriteLine("TRP 1");
                            }
                        }
                        // Note: We should be able to print global literal values
                        break;
                    case "WRITEC":
                        offSet = getLoc(returnStrArr[2]);
                        if (IsGlobal(symbolHashSet[returnStrArr[2]]))
                        {

                            tCodeLine += "LDB R3 " + returnStrArr[2];
                            sr.WriteLine(tCodeLine);
                            sr.WriteLine("TRP 3");
                        }
                        else if (offSet >= 0)
                        {
                            // We are here if we are writing a referance variable in a class
                            tCodeLine += "MOV R3 FP";
                            sr.WriteLine(tCodeLine);
                            sr.WriteLine("ADI R3 #-8");
                            sr.WriteLine("LDR R3 (R3)"); // Loads the objects heap address
                            sr.WriteLine("ADI R3 #" + offSet); // get location in heap
                            sr.WriteLine("LDB R3 (R3)"); // Loads the value at the heap location
                            sr.WriteLine("TRP 3");
                        }
                        else
                        {
                            if (returnStrArr[2][0] == 'R' && (symbolHashSet[returnStrArr[2]].lexeme.Contains('[') && symbolHashSet[returnStrArr[2]].lexeme.Contains(']')))
                            {
                                // HANDLE ARRAYS
                                tCodeLine += "MOV R3 FP";
                                sr.WriteLine(tCodeLine);
                                sr.WriteLine("ADI R3 #" + offSet); // temp var on AR
                                sr.WriteLine("LDR R3 (R3)"); // LOCATION IN HEAP
                                sr.WriteLine("LDB R3 (R3)"); // value in heap
                                sr.WriteLine("TRP 3");
                            }
                            else
                            {
                                // HANDLE REFS
                                tCodeLine += "MOV R3 FP";
                                sr.WriteLine(tCodeLine);
                                sr.WriteLine("ADI R3 #" + offSet);
                                sr.WriteLine("LDR R3 (R3)");
                                sr.WriteLine("LDB R3 (R3)");
                                sr.WriteLine("TRP 3");
                            }

                        }
                        // Note: We should be able to print global literal values
                        break;
                    case "RTN":
                        // Note that RTN does not return a value

                        tCodeLine += "LDR R5 (FP)";
                        sr.WriteLine(tCodeLine);
                        sr.WriteLine("JMP R5");
                        break;
                    case "RETURN":
                        // Note our Return value will be stored at FP after we get our return address meaning the SP should point to the return value of the method UNTIL we call a new method that overwrites that value

                        // Get our return
                        if (returnStrArr[2] != "this" && IsGlobal(symbolHashSet[returnStrArr[2]]))
                        {
                            tCodeLine += "LDR R3 " + returnStrArr[2];
                            sr.WriteLine(tCodeLine);
                        }
                        else if (returnStrArr[2] == "this") 
                        {
                            tCodeLine += "MOV R3 FP // RETURN THIS";
                            sr.WriteLine(tCodeLine);
                            sr.WriteLine("ADI R3 #-8");
                            sr.WriteLine("LDR R3 (R3)"); // Double check
                        }
                        else
                        {
                            tCodeLine += "MOV R3 FP";
                            sr.WriteLine(tCodeLine);
                            offSet = getLoc(returnStrArr[2]);
                            sr.WriteLine("ADI R3 #" + offSet);
                            sr.WriteLine("LDR R3 (R3)");
                        }
                        // Store the result at the FP (we leave the method so it is going to be overwritten by the next AR call anyway)
                        sr.WriteLine("LDR R5 (FP)");
                        sr.WriteLine("STR R3 (FP)");
                        sr.WriteLine("JMP R5");
                        break;
                    case "SUB":
                    case "MUL":
                    case "DIV":
                    case "ADD":
                        offSet = getLoc(returnStrArr[2]);
                        // get first op
                        if (IsGlobal(symbolHashSet[returnStrArr[2]]))
                        {

                            tCodeLine += "LDR R0 " + returnStrArr[2];
                            sr.WriteLine(tCodeLine);
                        }
                        else
                        {
                            tCodeLine += "MOV R0 FP";
                            sr.WriteLine(tCodeLine);
                            sr.WriteLine("ADI R0 #" + offSet);
                            sr.WriteLine("LDR R0 (R0)");
                        }
                        // get second op
                        offSet = getLoc(returnStrArr[3]);
                        if (IsGlobal(symbolHashSet[returnStrArr[3]]))
                        {
                            sr.WriteLine("LDR R1 " + returnStrArr[3]);
                        }
                        else
                        {
                            sr.WriteLine("MOV R1 FP");
                            sr.WriteLine("ADI R1 #" + offSet);
                            sr.WriteLine("LDR R1 (R1)");
                        }
                        // Perform action
                        if (returnStrArr[1] == "ADD") 
                        {
                            sr.WriteLine("ADD R0 R1");
                        } 
                        else if (returnStrArr[1] == "SUB") 
                        {
                            sr.WriteLine("SUB R0 R1");
                        }
                        else if (returnStrArr[1] == "MUL")
                        {
                            sr.WriteLine("MUL R0 R1");
                        }
                        else if (returnStrArr[1] == "DIV")
                        {
                            sr.WriteLine("DIV R0 R1");
                        }
                        // Store in temp var location
                        offSet = getLoc(returnStrArr[4]);
                        sr.WriteLine("MOV R2 FP");
                        sr.WriteLine("ADI R2 #" + offSet);
                        sr.WriteLine("STR R0 (R2)");
                        break;
                    case "LT":
                    case "GT":
                    case "EQ":
                    case "NE":
                    case "GE":
                    case "LE":
                        // get first op
                        offSet = getLoc(returnStrArr[2]);
                        if (IsGlobal(symbolHashSet[returnStrArr[2]]))
                        {

                            tCodeLine += "LDR R0 " + returnStrArr[2];
                            sr.WriteLine(tCodeLine);
                        }
                        else
                        {
                            tCodeLine += "LDR R0 FP";
                            sr.WriteLine(tCodeLine);
                            sr.WriteLine("ADI R0 #" + offSet);
                            sr.WriteLine("LDR R0 (R0)");
                        }
                        // get second op
                        offSet = getLoc(returnStrArr[3]);
                        if (IsGlobal(symbolHashSet[returnStrArr[3]]))
                        {
                            sr.WriteLine("LDR R1 " + returnStrArr[3]);
                        }
                        else
                        {
                            sr.WriteLine("LDR R1 FP");
                            sr.WriteLine("ADI R1 #" + offSet);
                            sr.WriteLine("LDR R1 (R1)");
                        }
                        // perform operation
                        sr.WriteLine("CMP R0 R1");
                        if (returnStrArr[1] == "GT")
                        {
                            sr.WriteLine("BGT R0 L" + internlLblCnt);
                        } 
                        else if (returnStrArr[1] == "LT") 
                        {
                            sr.WriteLine("BLT R0 L" + internlLblCnt);
                        }
                        else if (returnStrArr[1] == "EQ")
                        {
                            sr.WriteLine("BRZ R0 L" + internlLblCnt);
                        }
                        else if (returnStrArr[1] == "NE")
                        {
                            sr.WriteLine("BNZ R0 L" + internlLblCnt);
                        }
                        else if (returnStrArr[1] == "GE")
                        {
                            sr.WriteLine("BGT R0 L" + internlLblCnt);
                            sr.WriteLine("BRZ R0 L" + internlLblCnt);
                        }
                        else if (returnStrArr[1] == "LE")
                        {
                            sr.WriteLine("BLT R0 L" + internlLblCnt);
                            sr.WriteLine("BRZ R0 L" + internlLblCnt);
                        }
                        offSet = getLoc(returnStrArr[4]);
                        // Store false in temp var location
                        sr.WriteLine("LDR R0 false");
                        sr.WriteLine("MOV R2 FP");
                        sr.WriteLine("ADI R2 #" + offSet);
                        sr.WriteLine("STR R0 (R2)");
                        sr.WriteLine("JMP L" + (internlLblCnt + 1));
                        // Store true in temp var location
                        sr.WriteLine("L" + internlLblCnt + " LDR R0 true");
                        sr.WriteLine("MOV R2 FP");
                        sr.WriteLine("ADI R2 #" + offSet);
                        sr.WriteLine("STR R0 (R2)");
                        // Make next line for label
                        nextLbl = "L" + (internlLblCnt + 1);

                        internlLblCnt = internlLblCnt + 2;
                        break;
                    case "BF":
                        // get first op                        
                        offSet = getLoc(returnStrArr[2]);
                        if (IsGlobal(symbolHashSet[returnStrArr[2]]))
                        {
                            tCodeLine += "LDR R1 " + returnStrArr[2];
                            sr.WriteLine(tCodeLine);
                        }
                        else
                        {
                            tCodeLine += "LDR R1 FP";
                            sr.WriteLine(tCodeLine);
                            sr.WriteLine("ADI R1 #" + offSet);
                            sr.WriteLine("LDR R1 (R1)");
                        }
                        
                        sr.WriteLine("BRZ R0 " + returnStrArr[3]);
                        break;
                    case "PUSH":
                        // Get the param location
                        tCodeLine += "LDR R1 SP // PUSH PARAM";
                        sr.WriteLine(tCodeLine);
                        sr.WriteLine("ADI R1 #" + paramOffset);
                        paramOffset -= 4;
                        // Get the Value we want to store
                        if (returnStrArr[2] == "this") 
                        {
                            sr.WriteLine("LDR R0 FP");
                            sr.WriteLine("ADI R0 #-8");
                            sr.WriteLine("LDR R0 (R0)");

                            sr.WriteLine("MOV R3 R0");
                        }
                        else if (IsGlobal(symbolHashSet[returnStrArr[2]]))
                        {
                            sr.WriteLine("LDR R0 " + returnStrArr[2]);
                        }
                        else
                        {
                            offSet = getLoc(returnStrArr[2]);
                            sr.WriteLine("LDR R0 FP");
                            sr.WriteLine("ADI R0 #" + offSet);
                            sr.WriteLine("LDR R0 (R0)");
                        }
                        sr.WriteLine("STR R0 (R1) // END PUSH");
                        break;
                    case "PEEK":
                        // after a function call peek the top value and store it on the temp 29 var
                        tCodeLine += "MOV R3 SP // PEEK";
                        sr.WriteLine(tCodeLine);
                        // Get the return value
                        sr.WriteLine("LDR R2 (R3)");
                        // store at temp offset location 
                        offSet = getLoc(returnStrArr[2]);
                        sr.WriteLine("LDR R1 FP");
                        sr.WriteLine("ADI R1 #" + offSet);
                        sr.WriteLine("STR R2 (R1) // END PEEK");
                        // The value of the method call should now be stored at the temp var in the function
                        break;
                    case "JMP":
                        tCodeLine += "JMP " + returnStrArr[2]; // NOTE: ARSIZE SHOULD NEVER BE POSITIVE 
                        sr.WriteLine(tCodeLine);
                        break;
                    case "OR":
                        // get first op
                        offSet = getLoc(returnStrArr[2]);
                        if (IsGlobal(symbolHashSet[returnStrArr[2]]))
                        {

                            tCodeLine += "LDR R0 " + returnStrArr[2];
                            sr.WriteLine(tCodeLine);
                        }
                        else
                        {
                            tCodeLine += "LDR R0 FP";
                            sr.WriteLine(tCodeLine);
                            sr.WriteLine("ADI R0 #" + offSet);
                            sr.WriteLine("LDR R0 (R0)");
                        }
                        // get second op
                        offSet = getLoc(returnStrArr[3]);
                        if (IsGlobal(symbolHashSet[returnStrArr[3]]))
                        {
                            sr.WriteLine("LDR R1 " + returnStrArr[3]);
                        }
                        else
                        {
                            sr.WriteLine("LDR R1 FP");
                            sr.WriteLine("ADI R1 #" + offSet);
                            sr.WriteLine("LDR R1 (R1)");
                        }
                        // perform operation
                        sr.WriteLine("OR R0 R1");
                        offSet = getLoc(returnStrArr[4]);
                        // Store result
                        sr.WriteLine("MOV R2 FP");
                        sr.WriteLine("ADI R2 #" + offSet);
                        sr.WriteLine("STR R0 (R2)");
                        break;
                    case "AND":
                        // get first op
                        offSet = getLoc(returnStrArr[2]);
                        if (IsGlobal(symbolHashSet[returnStrArr[2]]))
                        {

                            tCodeLine += "LDR R0 " + returnStrArr[2];
                            sr.WriteLine(tCodeLine);
                        }
                        else
                        {
                            tCodeLine += "LDR R0 FP";
                            sr.WriteLine(tCodeLine);
                            sr.WriteLine("ADI R0 #" + offSet);
                            sr.WriteLine("LDR R0 (R0)");
                        }
                        // get second op
                        offSet = getLoc(returnStrArr[3]);
                        if (IsGlobal(symbolHashSet[returnStrArr[3]]))
                        {
                            sr.WriteLine("LDR R1 " + returnStrArr[3]);
                        }
                        else
                        {
                            sr.WriteLine("LDR R1 FP");
                            sr.WriteLine("ADI R1 #" + offSet);
                            sr.WriteLine("LDR R1 (R1)");
                        }
                        // perform operation
                        sr.WriteLine("AND R0 R1");
                        offSet = getLoc(returnStrArr[4]);
                        // Store result
                        sr.WriteLine("MOV R2 FP");
                        sr.WriteLine("ADI R2 #" + offSet);
                        sr.WriteLine("STR R0 (R2)");
                        break;
                    case "READI":
                        tCodeLine += "TRP 2";
                        // Store int at location
                        offSet = getLoc(returnStrArr[2]);
                        sr.WriteLine(tCodeLine);
                        sr.WriteLine("MOV R2 FP");
                        sr.WriteLine("ADI R2 #" + offSet);
                        sr.WriteLine("STR R3 (R2)");
                        break;
                    case "READC":
                        tCodeLine += "TRP 4";
                        // store char at location
                        offSet = getLoc(returnStrArr[2]);
                        sr.WriteLine(tCodeLine);
                        sr.WriteLine("MOV R2 FP");
                        sr.WriteLine("ADI R2 #" + offSet);
                        sr.WriteLine("STB R3 (R2)");
                        break;
                    case "NEWI":
                        // Ex: NEWI T30 12, store corrent heap location in T30 and push the heap pointer down 12 bytes
                        tCodeLine += "MOV R7 SL";
                        sr.WriteLine(tCodeLine);
                        offSet = Convert.ToInt32(returnStrArr[3]);

                        sr.WriteLine("ADI SL #" + offSet); // SL should INCREASE by the SIZE OF THE ARRAY
                        // get first op WE CANT STORE INTO GLOABLS SO OUR OFFSET SHOULD ONLY BE 0 IF IT IS THE FIRST LOCATION IN HEAP

                        offSet = getLoc(returnStrArr[2]);
                        sr.WriteLine("MOV R1 FP");
                        sr.WriteLine("ADI R1 #" + offSet);
                        // Store Heap Loc in temp var

                        sr.WriteLine("STR R7 (R1)");                        
                        offSet = getLoc(returnStrArr[2]);
                        break;
                    case "NEW":
                        // Ex: NEW T2 T1, the char array has the value at T1, T1's value needs to be added to SL and the SL needs to be stored at T2
                        tCodeLine += "MOV R7 SL";
                        sr.WriteLine(tCodeLine);
                        offSet = symbolHashSet[returnStrArr[3]].curOffset;
                        sr.WriteLine("LDR R1 FP");
                        sr.WriteLine("ADI R1 #" + offSet);
                        sr.WriteLine("LDR R1 (R1)"); // The size of the array as an int should be in R1 now

                        sr.WriteLine("ADD SL R1"); // The heap should increase by the size of the array
                        // get first op WE CANT STORE INTO GLOABLS SO OUR OFFSET SHOULD ONLY BE 0 IF IT IS THE FIRST LOCATION IN HEAP

                        offSet = getLoc(returnStrArr[2]); // this is the location of r2 and should have the SL orig (R7) stored at the location
                        sr.WriteLine("MOV R2 FP");
                        sr.WriteLine("ADI R2 #" + offSet);
                        // Store Heap Loc in temp var
                        sr.WriteLine("STR R7 (R2)");
                        offSet = getLoc(returnStrArr[2]);
                        break;

                    case "REF":

                        offSet = getLoc(returnStrArr[2]);
                        tCodeLine += "MOV R0 FP";
                        sr.WriteLine(tCodeLine);
                        sr.WriteLine("ADI R0 #" + offSet);
                        sr.WriteLine("LDR R0 (R0)"); // R0 now contains the address stored by the temp var
                        bool nestie = false;
                        if (symbolHashSet[returnStrArr[4]].lexeme.Contains(".")) 
                        {
                            nestie = true;
                            //sr.WriteLine("LDB R3 H109");
                            //sr.WriteLine("TRP 3");
                            //sr.WriteLine("TRP 3");
                            //sr.WriteLine("MOV R3 R0");
                            //sr.WriteLine("TRP 1");
                            //sr.WriteLine("LDB R3 H109");
                            //sr.WriteLine("TRP 3");
                            
                            // We enter this if we are getting a nested ref
                            sr.WriteLine("LDR R0 (R0)");

                            sr.WriteLine("LDB R3 H109");
                            //sr.WriteLine("TRP 3");
                            //sr.WriteLine("MOV R3 R0");
                            //sr.WriteLine("TRP 1");
                            //sr.WriteLine("LDB R3 H109");
                            //sr.WriteLine("TRP 3");
                        }
                        offSet = getLoc(returnStrArr[3]);
                        sr.WriteLine("ADI R0 #" + offSet); // R0 now points to the location on the heap where our data is specifically

                        if (symbolHashSet[returnStrArr[4]].lexeme.Contains("."))
                        {
                            //sr.WriteLine("LDB R3 H109");
                            //sr.WriteLine("TRP 3");
                            //sr.WriteLine("MOV R3 R0");
                            //sr.WriteLine("TRP 1");
                            //sr.WriteLine("LDB R3 H109");
                            //sr.WriteLine("TRP 3");
                            // sr.WriteLine("TRP 3");
                        }

                        // CHecking
                        // Get the data at the location
                        sr.WriteLine("LDR R1 (R0)"); // R1 now contains the value in the heap WE DONT NEED TO CHECK IF IT IS A CHAR BECAUSE IT STILL TAKES 4 BYTES ON AR, we just wont use whatever the other 3 bytes we get after the location are
                        
                        

                        // We now need to store the R0 value at the temporary Rtype offset
                        offSet = getLoc(returnStrArr[4]);
                        sr.WriteLine("MOV R2 FP");
                        sr.WriteLine("ADI R2 #" + offSet);
                        if (nestie) 
                        {
                            sr.WriteLine("STR R0 (R2)"); // we store the value at R1 into the location at R2, our referance in the AR now contains the val we want
                            break;
                        }
                        sr.WriteLine("STR R1 (R2)"); // we store the value at R1 into the location at R2, our referance in the AR now contains the val we want
                        
                        break;
                    case "AEF":
                        // Ex AEF L130 N126 R19 
                        offSet = getLoc(returnStrArr[2]);
                        if (offSet < 0)
                        {
                            tCodeLine += "MOV R0 FP";
                            sr.WriteLine(tCodeLine);
                            sr.WriteLine("ADI R0 #" + offSet);
                            sr.WriteLine("LDR R0 (R0)"); // R0 now contains the address of our var on the heap
                        }
                        else 
                        {
                            tCodeLine += "MOV R0 FP";
                            sr.WriteLine(tCodeLine);
                            sr.WriteLine("ADI R0 #-8");
                            sr.WriteLine("LDR R0 (R0)"); // we are now at the base address in heap of our object
                            sr.WriteLine("ADI R0 #" + offSet); // we are now at the stored address of out array in heap
                            sr.WriteLine("LDR R0 (R0)"); // we are now at the base address of array in heap
                        }

                        try
                        {
                            // using a Global to access the array
                            offSet = Convert.ToInt32(symbolHashSet[returnStrArr[3]].lexeme); // this should be the index which means we need offset * 4 for anything that is NOT a char
                            if (symbolHashSet[returnStrArr[2]].data[0][2] != "char")
                            {
                                offSet *= 4;
                            }
                            sr.WriteLine("ADI R0 #" + offSet); // R0 now points to the location on the heap where our data is specifically
                        }
                        catch (FormatException e)
                        {
                            offSet = getLoc(returnStrArr[3]);
                            // Our index is on the AR
                            if (symbolHashSet[returnStrArr[3]].curOffset < 0)
                            { 
                                sr.WriteLine("MOV R1 FP");
                                sr.WriteLine("ADI R1 #" + offSet);
                                sr.WriteLine("LDR R1 (R1)"); // We have the value of the index in R1

                                if (symbolHashSet[returnStrArr[2]].data[0][2] != "char")
                                {
                                    sr.WriteLine("LDR R3 N101");
                                    sr.WriteLine("MUL R1 R3"); // R0 is now the specific location in the array of our int or object address
                                }
                                sr.WriteLine("ADD R0 R1"); // R0 is now the specific location in the array of our int or object address
                            }
                            else 
                            {
                                // Our index is on the heap 
                                sr.WriteLine("MOV R1 FP");
                                sr.WriteLine("ADI R1 #-8");
                                sr.WriteLine("LDR R1 (R1)"); // we are now at the base address in heap
                                sr.WriteLine("ADI R1 #" + offSet); // we now have the address of out index
                                sr.WriteLine("LDR R1 (R1)"); // We have the value of the index in R1
                                sr.WriteLine("ADD R0 R1"); // R0 is now the specific location in the array
                            }
                        }

                        // Get the data at the location

                        // We now need to store the R0 location at the temporary Rtype offset
                        offSet = getLoc(returnStrArr[4]);
                        sr.WriteLine("MOV R2 FP");
                        sr.WriteLine("ADI R2 #" + offSet);

                        sr.WriteLine("STR R0 (R2)"); // we store the value at R1 into the location at R2, our referance in the AR now contains the val we want

                        break;
                    default:
                        throw new Exception("Unhandled Command: " + returnStrArr[1]);
                }
            }
        }

        public bool IsGlobal(Symbol s)
        {
            if ((s.symid == "true" || s.symid == "false" || s.symid == "null" || s.kind == "ilit" || s.kind == "clit") && (s.symid[0] != 'T' && s.symid[0] != 'R')) 
            {
                return true;
            }

            return false;
        }
    }
}
