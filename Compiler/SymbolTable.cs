using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace Compiler
{
    class SymbolTable
    {
        bool secondPass;
        public Dictionary<string, Symbol> symbolHashSet;
        public int symbolCounter = 100; // Part of the naming convention this counter increments only when we add something to the symbol table
        public int tempVarCounter = 1; // Might be used later 
        private ETparser parsie;
        List<string> scopeLevel;
        List<string> classes;
        Symbol addSymbol = null;
        List<Symbol> tempSymbols = null;
        StreamWriter symWriter;
        SAS semanticAnalyzer;
        // Icode offset tracking
        private int curClassOffset = 0;
        private int curMethodOffset = 0;
        private int byteSize = 0;
        string curLabel = "";
        string curClass = "";
        private bool constError = false;
        // Used to make sure we are counting inside a class properly even if we pass into a nested method
        private bool insideClass = false;
        // Used when we begin calculating offesets for a methods instance variables
        private bool insideMethod = false;
        private bool argList = false;
        private bool thisMemberFlag = false;
        // Used when finding a method to calculate temp variable size
        string methodScope;
        public QuadTable quad = new QuadTable();
        public QuadTable permQuadPointer;
        public QuadTable sQuad = null;
        bool exitIf = false;
        public SymbolTable()
        {
            permQuadPointer = quad;
            secondPass = false;
            symbolHashSet = new Dictionary<string, Symbol>();
            parsie = new ETparser();
            classes = new List<string>();
            scopeLevel = new List<string>();
            scopeLevel.Add("g"); // g is "global   
        }
        /*
            Execute method should take in our tokens and add a symbol to the symbol table id necessary
         */
        public void Execute()
        {
            parsie.Update();
            // Pre load symbol table
            addSymbol = new Symbol(scopeString(), "true", "true", "true");
            addSymbol.data[0].Add("type: ");
            addSymbol.data[0].Add("bool");
            addSymbol.data[1].Add("accessMod: ");
            addSymbol.data[1].Add("public");
            addSymbol.byteSize = 4;
            symbolHashSet.Add(addSymbol.symid, addSymbol);

            addSymbol = new Symbol(scopeString(), "false", "false", "false");
            addSymbol.data[0].Add("type: ");
            addSymbol.data[0].Add("bool");
            addSymbol.data[1].Add("accessMod: ");
            addSymbol.data[1].Add("public");
            addSymbol.byteSize = 4;

            symbolHashSet.Add(addSymbol.symid, addSymbol);

            addSymbol = new Symbol(scopeString(), "null", "null", "null");
            addSymbol.data[0].Add("type: ");
            addSymbol.data[0].Add("null");
            addSymbol.data[1].Add("accessMod: ");
            addSymbol.data[1].Add("public");
            addSymbol.byteSize = 4;

            symbolHashSet.Add(addSymbol.symid, addSymbol);

            // The Literal 4 is added BECAUSE IT IS THE SIZE OF A POINTER AND IS TO BE USED FOR ANY POINTERS EVEN IF the literal 4 is never used otherwise

            symbolCounter++;
            addSymbol = new Symbol("g", "N" + symbolCounter, "4", "ilit");
            addSymbol.data[0].Add("type: ");
            addSymbol.data[0].Add("int");
            addSymbol.data[1].Add("accessMod: ");
            addSymbol.data[1].Add("public");
            addSymbol.byteSize = 4;
            symbolHashSet.Add(addSymbol.symid, addSymbol);
            symbolCounter++;

            // The Literal 1 is added BECAUSE IT IS THE SIZE OF A char AND IS TO BE USED FOR ANY chars on heap EVEN IF the literal 1 is never used otherwise

            addSymbol = new Symbol("g", "N" + symbolCounter, "1", "ilit");
            addSymbol.data[0].Add("type: ");
            addSymbol.data[0].Add("int");
            addSymbol.data[1].Add("accessMod: ");
            addSymbol.data[1].Add("public");
            addSymbol.byteSize = 4;
            symbolHashSet.Add(addSymbol.symid, addSymbol);
            symbolCounter++;

            // The Literal 1 because out cin and needs to wait for enter before it breaks it's loop

            addSymbol = new Symbol("g", "N" + symbolCounter, "10", "ilit");
            addSymbol.data[0].Add("type: ");
            addSymbol.data[0].Add("int");
            addSymbol.data[1].Add("accessMod: ");
            addSymbol.data[1].Add("public");
            addSymbol.byteSize = 4;
            symbolHashSet.Add(addSymbol.symid, addSymbol);
            symbolCounter++;


            Compiliation_Unit();

            if (parsie.tokenArr[0].thisType != TokenType.EOF)
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "End of File");
            }

            /*
                Semnatic Analysis
             */
            secondPass = true;
            parsie.ETreader.Close();
            parsie = new ETparser();
            parsie.Update();
            semanticAnalyzer = new SAS(symbolHashSet, quad, classes);
            // SECOND PASS
            Compiliation_Unit();
            /*
                Print Symbol Table
             */
            /*symWriter = new StreamWriter("symTble.txt");

            foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
            {
                // Console.WriteLine(s.Value.ToString());
                symWriter.Write(s.Value.ToString());
            }

            symWriter.Close();*/

            /*
                Printe Quad Table
             */

            // quad.ToString();

            /*
                Generate T Code
             */
            TCodeGenerator tCodie = new TCodeGenerator(symbolHashSet, quad);
            tCodie.writeTCodeFile();
        }
        /*
            Scope to string
         */
        private string scopeString()
        {
            string retString = scopeLevel[0];
            for (int i = 1; i < scopeLevel.Count; i++)
            {
                retString += "." + scopeLevel[i];
            }
            return retString;
        }
        /*
            Compiliation Unit 
         */
        private void Compiliation_Unit()
        {
            if (secondPass)
            {
                // build initial call to main in quad table
                quad.AddRow("", "FRAME", "main", "null", "", "");
                quad.AddRow("", "CALL", "main", "", "", "");
                quad.AddRow("", "EXIT", "main", "", "", "");
            }
            // 0 or more instances of class Declarations
            while (parsie.tokenArr[0].lexeme == "class")
            {
                ClassDeclaration();
            }
            // main method
            parsie.commentLine = "";
            if (parsie.tokenArr[0].lexeme == "void")
            {
                parsie.Update();
                if (parsie.tokenArr[0].lexeme == "kxi2021")
                {
                    parsie.Update();
                    if (parsie.tokenArr[0].lexeme == "main")
                    {
                        addSymbol = new Symbol(scopeString(), "main", "main", "main");
                        methodScope = scopeString();
                        scopeLevel.Add("main");
                        if (secondPass)
                        {
                            semanticAnalyzer.scope = scopeString();
                        }

                        parsie.Update();
                        if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_OPEN)
                        {
                            parsie.Update();
                            if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_CLOSE)
                            {
                                addSymbol.data[0].Add("returnType: ");
                                addSymbol.data[0].Add("void");
                                addSymbol.data[1].Add("Param: ");
                                addSymbol.data[1].Add("[");
                                addSymbol.data[1].Add("]");
                                addSymbol.data[2].Add("accessMod: ");
                                addSymbol.data[2].Add("public");
                                if (!secondPass)
                                {
                                    symbolHashSet.Add(addSymbol.symid, addSymbol);
                                }
                                parsie.Update();
                                // Main has a return and pfp, it is unused but exists.
                                if (!secondPass)
                                {
                                    curMethodOffset = -12;
                                }
                                insideMethod = true;
                                if (secondPass)
                                {
                                    semanticAnalyzer.insideMethod = insideMethod;
                                }
                                Symbol methodPointer = addSymbol;
                                // if we are in the second pass set the methodPointer == to the current methods size so the offsets for the temp variables are accurate
                                if (secondPass)
                                {
                                    foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
                                    {
                                        if (s.Value.lexeme == methodPointer.lexeme && methodScope == s.Value.scope)
                                        {
                                            methodPointer = s.Value;
                                            curMethodOffset = s.Value.byteSize;
                                            semanticAnalyzer.tempVarOffset = curMethodOffset;
                                            break;
                                        }
                                    }
                                    quad.AddRow("main", "FUNC", "main", "", "", parsie.commentLine);
                                }
                                Method_body();

                                if (secondPass)
                                {
                                    methodPointer.byteSize = semanticAnalyzer.tempVarOffset; // final size with temps included
                                }
                                else
                                {
                                    methodPointer.byteSize = curMethodOffset; // it's actual size is byteSize *= -1;
                                }
                                insideMethod = false;
                                if (secondPass)
                                {
                                    semanticAnalyzer.insideMethod = insideMethod;
                                }
                                if (secondPass)
                                {
                                    quad.AddRow("", "RTN", "", "", "", "");
                                    //  quad.BackPatch("SKIPIF4","TESTLABEL"); BACK PATCH TEST UNCOMMENT AND CHANGE SKIPIF4 TO DESIRED TEST LABEL
                                }
                            }
                            else
                            {
                                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ")");
                            }

                        }
                        else
                        {
                            genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "(");
                        }
                        scopeLevel.RemoveAt(scopeLevel.Count - 1);
                        if (secondPass)
                        {
                            semanticAnalyzer.scope = scopeString();
                        }

                    }
                    else
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "main");
                    }
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "kxi2021");
                }
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "void");
            }
        }
        /*
            Class Declaration function
         */
        private void ClassDeclaration()
        {
            if (parsie.tokenArr[0].lexeme == "class")
            {
                /*
                    Begin making sQuad
                 */
                if (secondPass)
                {
                    sQuad = new QuadTable();
                    quad = sQuad; // remember our main quad is stored in permSquadPointer
                    semanticAnalyzer.quad = sQuad;
                    sQuad.AddRow(parsie.tokenArr[1].lexeme + "StaticInit", "FUNC", parsie.tokenArr[1].lexeme + "StaticInit", "", "", "");
                    // add staticInit method to the symTable
                    Symbol s = new Symbol(scopeString(), parsie.tokenArr[1].lexeme + "StaticInit", parsie.tokenArr[1].lexeme + "StaticInit", "method");
                    s.byteSize = -12;
                    symbolHashSet.Add(parsie.tokenArr[1].lexeme + "StaticInit", s);

                }
                parsie.Update();
                insideClass = true;
                if (parsie.tokenArr[0].thisType == TokenType.IDENTIFIER)
                {
                    if (!classes.Contains(parsie.tokenArr[0].lexeme))
                    {
                        curClass = parsie.tokenArr[0].lexeme;
                        classes.Add(parsie.tokenArr[0].lexeme);
                        // New class start offset at 0
                        curClassOffset = 0;
                        // Set class scope and add symbol to Table
                        addSymbol = new Symbol(scopeString(), "C" + symbolCounter, parsie.tokenArr[0].lexeme, "class");
                        Symbol classPointer = addSymbol;

                        if (!secondPass)
                        {
                            symbolHashSet.Add(addSymbol.symid, addSymbol);
                            symbolCounter++;
                        }
                        // Add Symbol
                        string curLevel = parsie.tokenArr[0].lexeme;
                        scopeLevel.Add(curLevel);
                        if (secondPass)
                        {
                            semanticAnalyzer.scope = scopeString();
                        }
                        parsie.Update();
                        if (parsie.tokenArr[0].thisType == TokenType.BLOCK_BEGIN)
                        {
                            parsie.Update();
                            while (parsie.tokenArr[0].thisType != TokenType.BLOCK_END)
                            {
                                Class_Member_Declaration();
                            }

                            if (parsie.tokenArr[0].thisType == TokenType.BLOCK_END)
                            {
                                parsie.Update();
                            }
                            else
                            {
                                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "}");
                            }
                            // at this point a class should have all members sizes included, we assign the size and then reset the offset for the next classs
                            classPointer.byteSize = curClassOffset;
                            curClassOffset = 0;
                            scopeLevel.RemoveAt(scopeLevel.Count - 1);
                            if (secondPass)
                            {
                                semanticAnalyzer.scope = scopeString();
                            }
                        }
                        else
                        {
                            genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "{");
                        }

                        curClass = "";
                    }
                    else if (classes.Contains(parsie.tokenArr[0].lexeme) && secondPass)
                    {
                        curClass = parsie.tokenArr[0].lexeme;
                        // Proceed in scope
                        string curLevel = parsie.tokenArr[0].lexeme;
                        scopeLevel.Add(curLevel);
                        if (secondPass)
                        {
                            semanticAnalyzer.scope = scopeString();
                        }
                        parsie.Update();
                        if (parsie.tokenArr[0].thisType == TokenType.BLOCK_BEGIN)
                        {
                            parsie.Update();
                            while (parsie.tokenArr[0].thisType != TokenType.BLOCK_END)
                            {
                                Class_Member_Declaration();
                            }

                            if (parsie.tokenArr[0].thisType == TokenType.BLOCK_END)
                            {
                                parsie.Update();
                            }
                            else
                            {
                                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "}");
                            }

                            scopeLevel.Remove(curLevel);
                            if (secondPass)
                            {
                                semanticAnalyzer.scope = scopeString();
                            }
                        }
                        else
                        {
                            genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "{");
                        }

                        curClass = "";
                    }
                    else
                    {
                        // DuplicateClasses
                        genSAMClassesDup(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme);
                    }
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "identifier");
                }
                insideClass = false;
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "class");
            }
            if (secondPass)
            {
                sQuad.AddRow("", "RTN", "", "", "", "");
                quad = permQuadPointer;
                semanticAnalyzer.quad = permQuadPointer;
                quad.addQuad(sQuad);
                sQuad = null;
            }
            /*write sQuad to quad and change pointer back to */
        }
        /*
            Class Member Declaration function
         */
        private void Class_Member_Declaration()
        {
            if (parsie.tokenArr[0].lexeme == "public" || parsie.tokenArr[0].lexeme == "private")
            {
                // Begin Symbol Creation
                addSymbol = new Symbol(scopeString(), "", "", "");
                addSymbol.data[0].Add("type: ");
                addSymbol.data[1].Add("accessMod: ");
                addSymbol.data[1].Add(parsie.tokenArr[0].lexeme);
                parsie.commentLine = "";
                parsie.Update();
                if (parsie.tokenArr[0].thisType == TokenType.IDENTIFIER || parsie.tokenArr[0].lexeme == "int" || parsie.tokenArr[0].lexeme == "bool" || parsie.tokenArr[0].lexeme == "char" || parsie.tokenArr[0].lexeme == "sym" || parsie.tokenArr[0].lexeme == "void")
                {
                    if (secondPass && parsie.tokenArr[0].thisType == TokenType.IDENTIFIER)
                    {
                        if (!classes.Contains(parsie.tokenArr[0].lexeme))
                        {
                            genErrorTExist(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme);
                        }
                    }
                    // Everything takes up 4 bytes, aside from chars. Arrays and class objects will have a 4 byte reference to their location in heap
                    byteSize = 4;
                    if (parsie.tokenArr[0].lexeme == "char")
                    {
                        byteSize = 1;
                    }
                    addSymbol.data[0].Add(parsie.tokenArr[0].lexeme);
                    // Check if it is an array
                    if (parsie.tokenArr[1].thisType == TokenType.ARRAY_BEGIN)
                    {
                        if (secondPass)
                        {
                            if (!classes.Contains(parsie.tokenArr[0].lexeme) && !(parsie.tokenArr[0].lexeme == "int" || parsie.tokenArr[0].lexeme == "bool" || parsie.tokenArr[0].lexeme == "char" || parsie.tokenArr[0].lexeme == "sym" || parsie.tokenArr[0].lexeme == "void"))
                            {
                                // TExists
                                genErrorSAMType(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme);
                            }
                        }
                        parsie.Update();
                        parsie.Update();
                        if (parsie.tokenArr[0].thisType == TokenType.ARRAY_END)
                        {
                            byteSize = 4; // if we have a char array we need the byte size to be 4 (address location)
                            parsie.Update();
                        }
                        else
                        {
                            genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "]");
                        }
                        addSymbol.data[0].Insert(1, "@:");
                    }
                    else
                    {
                        parsie.Update();
                    }
                    // Check Identifier
                    string idenName = "";
                    if (parsie.tokenArr[0].thisType == TokenType.IDENTIFIER)
                    {
                        // NOTE: This is where our scope would be defined for field declaration
                        addSymbol.lexeme = parsie.tokenArr[0].lexeme;
                        idenName = parsie.tokenArr[0].lexeme;
                        if (secondPass)
                        {
                            semanticAnalyzer.VPush(parsie.tokenArr[0], scopeLevel);
                        }
                        parsie.Update();
                    }
                    else
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "identifier");
                    }

                    Field_Declaration(idenName);
                    // Pop the field (methods stay on the stack)
                    if (secondPass)
                    {
                        semanticAnalyzer.EoE(parsie.tokenArr[0].lineNum);// VERIFY        
                    }
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "type");
                }
            }
            else if (parsie.tokenArr[0].thisType == TokenType.IDENTIFIER)
            {
                // Enter Constructor
                tempSymbols = new List<Symbol>();
                // Begin Symbol Creation
                addSymbol = new Symbol(scopeString(), "X" + symbolCounter, parsie.tokenArr[0].lexeme, "constructor");
                // Check for Duplicate
                int constructorCount = 0;
                if (constError)
                {
                    genSAMErrorDupConst(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme);
                }

                if (secondPass)
                {
                    foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
                    {
                        if (s.Value.lexeme == parsie.tokenArr[0].lexeme && s.Value.symid[0] == 'X')
                        {
                            constructorCount++;
                        }
                        if (constructorCount > 1)
                        {
                            constError = true;
                        }
                    }
                }

                if (!secondPass)
                {
                    symbolCounter++;
                }
                addSymbol.data[0].Add("returnType: ");
                addSymbol.data[0].Add(parsie.tokenArr[0].lexeme);
                addSymbol.data[1].Add("param: [");
                addSymbol.curOffset = curClassOffset;
                Constructor_Declaration();

                foreach (Symbol s in tempSymbols)
                {

                    if (!secondPass)
                    {
                        symbolHashSet.Add(s.symid, s);
                    }
                }
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "modifier, identifier, }");
            }
        }
        /*
            Constructor Declaration
         */
        private void Constructor_Declaration()
        {
            if (parsie.tokenArr[0].thisType == TokenType.IDENTIFIER)
            {

                string idenName = addSymbol.lexeme;
                if (secondPass)
                {
                    if (idenName != curClass)
                    {
                        genSAMConst(parsie.tokenArr[0].lineNum, idenName, curClass);
                    }
                    semanticAnalyzer.insideConstructor = true;
                    quad = permQuadPointer;
                    semanticAnalyzer.quad = permQuadPointer;

                    string constSymid = FindConstbyLexeme(idenName);
                    quad.AddRow(constSymid, "FUNC", constSymid, "", "", "");
                    semanticAnalyzer.commentRow = quad.GetBotRow();
                    semanticAnalyzer.firstRowComment = false;
                    quad.AddRow("", "FRAME", idenName + "StaticInit", "this", "", "");
                    quad.AddRow("", "CALL", idenName + "StaticInit", "", "", "");
                }
                methodScope = scopeString();
                if (!secondPass)
                {
                    curMethodOffset = -12;
                }
                scopeLevel.Add(idenName);
                if (secondPass)
                {
                    semanticAnalyzer.scope = scopeString();
                }
                string curLevel = parsie.tokenArr[0].lexeme;
                int curLine = parsie.tokenArr[0].lineNum;
                if (parsie.tokenArr[0].lexeme != scopeLevel[scopeLevel.Count - 1])
                {
                    genSAMConst(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, scopeLevel[scopeLevel.Count - 1]);
                }
                parsie.Update();
                if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_OPEN && parsie.tokenArr[1].thisType != TokenType.PARENTHESES_CLOSE)
                {
                    parsie.Update();
                    Parameter_List();

                    addSymbol.data[1].RemoveAt(addSymbol.data[1].Count - 1);
                    addSymbol.data[1].Add("]");

                    if (!secondPass)
                    {
                        foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
                        {
                            if (addSymbol.lexeme != s.Value.lexeme)
                            {
                                continue;
                            }
                            else
                            {
                                // Check Scope 
                                if (s.Value.scope != addSymbol.scope)
                                {
                                    continue;
                                }
                                // Verify they are both constructors
                                if (s.Value.symid[0] == addSymbol.symid[0])
                                {
                                    // verify params are different
                                    if (s.Value.data[1].Count == addSymbol.data[1].Count)
                                    {
                                        bool dupFlag = false;
                                        int tempCounter = 0;
                                        for (int i = 1; i < addSymbol.data[1].Count; i = i + 2)
                                        {
                                            if (tempSymbols[0].data[0][1] == symbolHashSet[s.Value.data[1][i]].data[0][1])
                                            {
                                                dupFlag = true;
                                            }
                                            else
                                            {
                                                dupFlag = false;
                                                break;
                                            }
                                            tempCounter++;
                                        }

                                        if (dupFlag)
                                        {
                                            genSAMErrorDupConst(curLine, scopeLevel[scopeLevel.Count - 1]);
                                        }
                                    }
                                    else
                                    {
                                        genSAMErrorDupConst(curLine, scopeLevel[scopeLevel.Count - 1]);
                                    }
                                }
                            }
                        }
                        symbolHashSet.Add(addSymbol.symid, addSymbol);
                        // Prevent Duplicate constructors
                    }
                    if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_CLOSE)
                    {
                        parsie.Update();
                    }
                    else
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ") ,");
                    }
                    // CONSTRUCTOR 
                    insideMethod = true;
                    if (secondPass)
                    {
                        semanticAnalyzer.insideMethod = insideMethod;
                    }

                    Symbol methodPointer = addSymbol;
                    // if we are in the second pass set the methodPointer == to the current methods size so the offsets for the temp variables are accurate
                    if (secondPass)
                    {
                        foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
                        {
                            if (s.Value.lexeme == methodPointer.lexeme && methodScope == s.Value.scope)
                            {
                                methodPointer = s.Value;
                                curMethodOffset = s.Value.byteSize;
                                semanticAnalyzer.tempVarOffset = curMethodOffset;
                                break;
                            }
                        }
                        semanticAnalyzer.commentRow[5] = parsie.commentLine;
                        parsie.commentLine = "";
                        semanticAnalyzer.firstRowComment = true;
                    }
                    Method_body();

                    if (secondPass && quad.GetBotRow()[1].ToString() != "RETURN")
                    {
                        quad.AddRow("", "RETURN", "this", "", "", "");
                    }

                    if (secondPass)
                    {
                        methodPointer.byteSize = semanticAnalyzer.tempVarOffset; // final size with temps included
                    }
                    else
                    {
                        methodPointer.byteSize = curMethodOffset; // it's actual size is byteSize *= -1;
                    }

                    insideMethod = false;
                    if (secondPass)
                    {
                        semanticAnalyzer.insideMethod = insideMethod;
                        semanticAnalyzer.insideConstructor = false;
                        quad = sQuad;
                        semanticAnalyzer.quad = sQuad;
                    }
                }
                else if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_OPEN && parsie.tokenArr[1].thisType == TokenType.PARENTHESES_CLOSE)
                {
                    addSymbol.data[1].Add("]");

                    if (!secondPass)
                    {
                        foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
                        {
                            if (addSymbol.lexeme != s.Value.lexeme)
                            {
                                continue;
                            }
                            else
                            {
                                // Check Scope 
                                if (s.Value.scope != addSymbol.scope)
                                {
                                    continue;
                                }
                                // Verify they are both constructors
                                if (s.Value.symid[0] == addSymbol.symid[0])
                                {
                                    // verify params are different
                                    if (s.Value.data[1].Count == addSymbol.data[1].Count)
                                    {
                                        bool dupFlag = false;
                                        int tempCounter = 0;
                                        for (int i = 1; i < addSymbol.data[1].Count; i = i + 2)
                                        {
                                            if (tempSymbols.Count == 0 && s.Value.data[1][i] == "]")
                                            {
                                                dupFlag = true;
                                            }
                                            else if (tempSymbols[0].data[0][1] == symbolHashSet[s.Value.data[1][i]].data[0][1])
                                            {
                                                dupFlag = true;
                                            }
                                            else
                                            {
                                                dupFlag = true;
                                                break;
                                            }
                                            tempCounter++;
                                        }

                                        if (dupFlag)
                                        {
                                            genSAMErrorDupConst(curLine, scopeLevel[scopeLevel.Count - 1]);
                                        }
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                        }
                        symbolHashSet.Add(addSymbol.symid, addSymbol);
                        // Prevent Duplicate constructors
                    }

                    parsie.Update();
                    parsie.Update();
                    // CONSTRUCTOR 
                    // Entering Method Body reset methodOffset to -12 (0 is ret -4 is PFP -8 is this) and flag we are inside a method
                    insideMethod = true;
                    if (secondPass)
                    {
                        semanticAnalyzer.insideMethod = insideMethod;
                    }
                    Symbol methodPointer = addSymbol;
                    // if we are in the second pass set the methodPointer == to the current methods size so the offsets for the temp variables are accurate
                    if (secondPass)
                    {
                        foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
                        {
                            if (s.Value.lexeme == methodPointer.lexeme && methodScope == s.Value.scope)
                            {
                                methodPointer = s.Value;
                                curMethodOffset = s.Value.byteSize;
                                semanticAnalyzer.tempVarOffset = curMethodOffset;
                                break;
                            }
                        }
                    }
                    Method_body();
                    if (secondPass && quad.GetBotRow()[1].ToString() != "RETURN")
                    {
                        quad.AddRow("", "RETURN", "this", "", "", "");
                    }
                    // in the first pass we set it to the size without temps, in the second pass we add temps

                    if (secondPass)
                    {
                        methodPointer.byteSize = semanticAnalyzer.tempVarOffset; // final size with temps includedvoid method_Body
                    }
                    else
                    {
                        methodPointer.byteSize = curMethodOffset; // it's actual size is byteSize *= -1;
                    }

                    insideMethod = false;
                    if (secondPass)
                    {
                        semanticAnalyzer.insideMethod = insideMethod;
                    }
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "(");
                }

                scopeLevel.RemoveAt(scopeLevel.Count - 1);
                if (secondPass)
                {
                    semanticAnalyzer.scope = scopeString();
                }
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "classname");
            }

            if (secondPass)
            {
                quad = sQuad;
                semanticAnalyzer.quad = sQuad;
            }
        }
        /*
            Field Declaration Method
         */
        private void Field_Declaration(string idenName)
        {
            if (parsie.tokenArr[0].lexeme == "=")
            {
                if (secondPass)
                {
                    semanticAnalyzer.OPush(parsie.tokenArr[0]);
                }
                parsie.Update();
                // Complete Addition of ivar symbol
                addSymbol.kind = "ivar";
                addSymbol.symid = "V" + symbolCounter;
                if (!secondPass)
                {
                    symbolCounter++;
                    addSymbol.byteSize = byteSize;
                    if (insideClass)
                    {
                        addSymbol.curOffset = curClassOffset;
                        curClassOffset += byteSize;
                    }
                    symbolHashSet.Add(addSymbol.symid, addSymbol);
                }

                // Assignment
                AssignmentExpression();
                if (parsie.tokenArr[0].lexeme == ";")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.comment = parsie.commentLine;
                        parsie.commentLine = "";
                        semanticAnalyzer.EoE(parsie.tokenArr[0].lineNum);
                        semanticAnalyzer.firstRowComment = true;
                    }
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ";");
                }
            }
            else if (parsie.tokenArr[0].lexeme == "(")
            {
                // Verify this method is not the same name as class ()
                if (idenName == curClass)
                {
                    genError(parsie.tokenArr[0].lineNum, idenName, "identifier");
                }
                else if (secondPass && classes.Contains(idenName))
                {
                    genError(parsie.tokenArr[0].lineNum, idenName, "identifier");
                }
                // Write the func start                
                parsie.Update();
                if (parsie.tokenArr[0].lexeme == ")")
                {
                    parsie.Update();
                    addSymbol.kind = "method";
                    addSymbol.symid = "M" + symbolCounter;
                    if (!secondPass)
                    {
                        symbolCounter++;
                    }
                    else
                    {
                        /*
                         find the method here
                         */
                        string s = FindMethodSymId(scopeString(), idenName);
                        if (s != "")
                        {
                            quad = permQuadPointer;
                            semanticAnalyzer.quad = permQuadPointer;
                            quad.AddRow(s, "FUNC", s, "", "", parsie.commentLine);
                            parsie.commentLine = "";
                        }
                    }
                    addSymbol.data[2] = addSymbol.data[1];
                    addSymbol.data[1] = new List<string>();
                    addSymbol.data[1].Add("Param: [");
                    if (insideClass)
                    {
                        addSymbol.curOffset = curClassOffset;
                    }

                    methodScope = scopeString();
                    scopeLevel.Add(idenName);
                    if (secondPass)
                    {
                        semanticAnalyzer.scope = scopeString();
                    }
                    addSymbol.data[1].Add("]");

                    if (!secondPass)
                    {
                        if (insideClass)
                        {
                            addSymbol.curOffset = curClassOffset;
                        }
                        symbolHashSet.Add(addSymbol.symid, addSymbol);
                    }
                    // Entering Method Body reset methodOffset to -12 (0 is ret -4 is PFP -8 is this) and flag we are inside a method
                    if (!secondPass)
                    {
                        curMethodOffset = -12;
                    }

                    insideMethod = true;
                    if (secondPass)
                    {
                        semanticAnalyzer.insideMethod = insideMethod;
                    }

                    Symbol methodPointer = addSymbol;
                    // if we are in the second pass set the methodPointer == to the current methods size so the offsets for the temp variables are accurate
                    if (secondPass)
                    {
                        foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
                        {
                            if (s.Value.lexeme == methodPointer.lexeme && methodScope == s.Value.scope)
                            {
                                methodPointer = s.Value;
                                curMethodOffset = s.Value.byteSize;
                                semanticAnalyzer.tempVarOffset = curMethodOffset;
                                break;
                            }
                        }
                    }

                    Method_body();
                    // If not return put a 
                    if (secondPass)
                    {
                        if (!(quad.GetBotRow()[1].ToString() == "RETURN" || quad.GetBotRow()[1].ToString() == "RTN"))
                        {
                            quad.AddRow("", "RTN", "", "", "", "");
                        }
                        quad = sQuad;
                        semanticAnalyzer.quad = sQuad;
                    }
                    if (secondPass)
                    {
                        methodPointer.byteSize = semanticAnalyzer.tempVarOffset; // final size with temps included
                    }
                    else
                    {
                        methodPointer.byteSize = curMethodOffset; // it's actual size is byteSize *= -1;
                    }

                    insideMethod = false;
                    if (secondPass)
                    {
                        semanticAnalyzer.insideMethod = insideMethod;
                    }

                    scopeLevel.RemoveAt(scopeLevel.Count - 1);
                    if (secondPass)
                    {
                        semanticAnalyzer.scope = scopeString();
                    }
                }
                else
                {
                    addSymbol.kind = "method";
                    addSymbol.symid = "M" + symbolCounter;
                    DataRow commentRow = null;
                    if (!secondPass)
                    {
                        symbolCounter++;
                    }
                    else
                    {
                        string s = FindMethodSymId(scopeString(), idenName);
                        if (s != "")
                        {
                            quad = permQuadPointer;
                            semanticAnalyzer.quad = permQuadPointer;
                            quad.AddRow(s, "FUNC", s, "", "", parsie.commentLine);
                            commentRow = quad.GetBotRow();
                            parsie.commentLine = "";
                        }
                    }
                    addSymbol.data[2] = addSymbol.data[1];
                    addSymbol.data[1] = new List<string>();
                    addSymbol.data[1].Add("Param: [");
                    if (insideClass)
                    {
                        addSymbol.curOffset = curClassOffset;
                    }
                    // Param List add to symbol table
                    methodScope = scopeString();
                    scopeLevel.Add(idenName);
                    if (secondPass)
                    {
                        semanticAnalyzer.scope = scopeString();
                    }
                    // set function size to base -12 increase based on paramaters and contained instance variables and temp variables
                    if (!secondPass)
                    {
                        curMethodOffset = -12;
                    }

                    insideMethod = true;
                    if (secondPass)
                    {
                        semanticAnalyzer.insideMethod = insideMethod;
                    }

                    Parameter_List();
                    if (parsie.tokenArr[0].lexeme == ")")
                    {
                        parsie.Update();
                        if (secondPass)
                        {
                            commentRow[5] = commentRow[5].ToString() + parsie.commentLine.Remove(0, 3);
                        }
                    }
                    else
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ") ,");
                    }
                    addSymbol.data[1].RemoveAt(addSymbol.data[1].Count - 1);
                    addSymbol.data[1].Add("]");

                    if (!secondPass)
                    {
                        symbolHashSet.Add(addSymbol.symid, addSymbol);
                    }
                    // Entering Method Body reset methodOffset to -12 (0 is ret -4 is PFP -8 is this) and flag we are inside a method

                    Symbol methodPointer = addSymbol;
                    // if we are in the second pass set the methodPointer == to the current methods size so the offsets for the temp variables are accurate
                    if (secondPass)
                    {
                        foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
                        {
                            if (s.Value.lexeme == methodPointer.lexeme && methodScope == s.Value.scope)
                            {
                                methodPointer = s.Value;
                                curMethodOffset = s.Value.byteSize;
                                semanticAnalyzer.tempVarOffset = curMethodOffset;
                                break;
                            }
                        }
                    }
                    Method_body();
                    if (secondPass)
                    {
                        methodPointer.byteSize = semanticAnalyzer.tempVarOffset; // final size with temps included
                    }
                    else
                    {
                        methodPointer.byteSize = curMethodOffset; // it's actual size is byteSize *= -1;
                    }

                    insideMethod = false;
                    if (secondPass)
                    {
                        if (!(quad.GetBotRow()[1].ToString() == "RETURN" || quad.GetBotRow()[1].ToString() == "RTN"))
                        {
                            quad.AddRow("", "RTN", "", "", "", "");
                        }
                        quad = sQuad;
                        semanticAnalyzer.quad = sQuad;

                        semanticAnalyzer.insideMethod = insideMethod;
                    }

                    scopeLevel.RemoveAt(scopeLevel.Count - 1);
                    if (secondPass)
                    {
                        semanticAnalyzer.scope = scopeString();
                    }

                    if (!secondPass)
                    {
                        foreach (Symbol s in tempSymbols)
                        {
                            symbolHashSet.Add(s.symid, s);
                        }
                    }
                    else
                    {
                        semanticAnalyzer.comment = parsie.commentLine;
                        parsie.commentLine = "";
                        semanticAnalyzer.EoE(parsie.tokenArr[0].lineNum);
                    }
                }

            }
            else if (parsie.tokenArr[0].lexeme == ";")
            {
                // Complete Addition of ivar symbol HEREFLAG
                addSymbol.kind = "ivar";
                addSymbol.symid = "V" + symbolCounter;
                if (!secondPass)
                {
                    symbolCounter++;
                    addSymbol.byteSize = byteSize;
                    if (insideClass)
                    {
                        addSymbol.curOffset = curClassOffset;
                        curClassOffset += byteSize;
                    }
                    symbolHashSet.Add(addSymbol.symid, addSymbol);
                }
                else
                {
                    semanticAnalyzer.comment = parsie.commentLine;
                    parsie.commentLine = "";
                    semanticAnalyzer.EoE(parsie.tokenArr[0].lineNum);
                }
                parsie.Update();
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "= ; (");
            }
        }
        /*
            Parameter List Function
         */
        private void Parameter_List()
        {
            tempSymbols = new List<Symbol>(); // reset for next list
            Parameter();
            while (parsie.tokenArr[0].lexeme == ",")
            {
                parsie.Update();
                Parameter();
            }
        }
        /*
            Parameter Function
         */
        private void Parameter()
        {
            // add to Addsymbol Data + Symbol table
            Symbol tempieSym = new Symbol(scopeString(), "P" + symbolCounter, "", "param");
            tempieSym.data[1].Add("accessMod: ");
            tempieSym.data[1].Add("private");
            if (!secondPass)
            {
                symbolCounter++;
            }
            if (parsie.tokenArr[0].thisType == TokenType.IDENTIFIER || parsie.tokenArr[0].lexeme == "int" || parsie.tokenArr[0].lexeme == "bool" || parsie.tokenArr[0].lexeme == "char" || parsie.tokenArr[0].lexeme == "sym" || parsie.tokenArr[0].lexeme == "void")
            {
                tempieSym.data[0].Add("type: ");
                tempieSym.data[0].Add(parsie.tokenArr[0].lexeme);
                // TExists
                if (secondPass && !classes.Contains(parsie.tokenArr[0].lexeme) && parsie.tokenArr[0].lexeme != "int" && parsie.tokenArr[0].lexeme != "bool" && parsie.tokenArr[0].lexeme != "char" && parsie.tokenArr[0].lexeme != "sym" && parsie.tokenArr[0].lexeme != "void")
                {
                    genErrorTExist(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme);
                }
                parsie.Update();
                // Check if it is an array
                if (parsie.tokenArr[0].thisType == TokenType.ARRAY_BEGIN)
                {
                    parsie.Update();
                    if (parsie.tokenArr[0].thisType == TokenType.ARRAY_END)
                    {
                        parsie.Update();
                    }
                    else
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "]");
                    }
                    tempieSym.data[0].Insert(1, "@:");
                }
                // Check Identifier
                if (parsie.tokenArr[0].thisType == TokenType.IDENTIFIER)
                {
                    tempieSym.lexeme = parsie.tokenArr[0].lexeme;
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "identifier");
                }
                // calculate offsets
                if (!secondPass)
                {
                    tempieSym.byteSize = 4;
                    tempieSym.curOffset = curMethodOffset;
                    curMethodOffset -= 4;
                }
                tempSymbols.Add(tempieSym);
                addSymbol.data[1].Add(tempieSym.symid);
                addSymbol.data[1].Add(",");
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "type");
            }
        }
        /*
            Method Body Function for method body
         */
        private void Method_body()
        {
            if (parsie.tokenArr[0].thisType == TokenType.BLOCK_BEGIN)
            {
                parsie.Update();
                if (secondPass)
                {
                    semanticAnalyzer.methodCall = false;
                    semanticAnalyzer.firstRowComment = true;
                }
                // 0 or more variable declaration
                while ((parsie.tokenArr[0].thisType == TokenType.IDENTIFIER || parsie.tokenArr[0].lexeme == "int" || parsie.tokenArr[0].lexeme == "bool" || parsie.tokenArr[0].lexeme == "char" || parsie.tokenArr[0].lexeme == "sym" || parsie.tokenArr[0].lexeme == "void") && parsie.tokenArr[1].lexeme != "=")
                {
                    if (parsie.tokenArr[1].thisType == TokenType.ARRAY_BEGIN && parsie.tokenArr[2].thisType != TokenType.ARRAY_END)
                    {
                        break;
                    }
                    if (parsie.tokenArr[1].lexeme == ".")
                    {
                        break;
                    }
                    if (parsie.tokenArr[1].lexeme == "(")
                    {
                        break;
                    }
                    else
                    {
                        Var_Declaration();
                    }
                }
                // 0 or more statements
                while (IsStatementStart(parsie.tokenArr[0]))
                {
                    Statement();
                }

                if (parsie.tokenArr[0].thisType == TokenType.BLOCK_END)
                {
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "}");
                }
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "{");
            }
        }
        /*
            Variable Declaration function 
         */
        private void Var_Declaration()
        {
            addSymbol = new Symbol(scopeString(), "L" + symbolCounter, "", "lvar");
            if (!secondPass)
            {
                symbolCounter++;
            }
            else
            {
                parsie.commentLine = "";
            }

            addSymbol.data[0].Add("type: ");
            addSymbol.data[1].Add("accessMod: ");
            addSymbol.data[1].Add("private");
            if (parsie.tokenArr[0].thisType == TokenType.KEYWORDS)
            {
                if (parsie.tokenArr[0].lexeme == "int" || parsie.tokenArr[0].lexeme == "char" || parsie.tokenArr[0].lexeme == "bool" || parsie.tokenArr[0].lexeme == "void" || parsie.tokenArr[0].lexeme == "sym")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.TPush(parsie.tokenArr[0].lexeme);
                    }
                    addSymbol.data[0].Add(parsie.tokenArr[0].lexeme);
                    parsie.Update();
                    if (parsie.tokenArr[0].thisType == TokenType.ARRAY_BEGIN)
                    {
                        parsie.Update();
                        if (parsie.tokenArr[0].thisType == TokenType.ARRAY_END)
                        {
                            if (secondPass)
                            {
                                semanticAnalyzer.intializeArr = true;
                            }
                            addSymbol.data[0].Insert(1, "@:");
                            parsie.Update();
                        }
                        else
                        {
                            genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "]");
                        }
                    }
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "Type keyword");
                }
            }
            else if (parsie.tokenArr[0].thisType == TokenType.IDENTIFIER)
            {
                if (secondPass)
                {
                    if (!classes.Contains(parsie.tokenArr[0].lexeme))
                    {
                        // TExists
                        genErrorSAMType(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme);
                    }
                    if (secondPass)
                    {
                        semanticAnalyzer.TPush(parsie.tokenArr[0].lexeme);
                    }
                }
                addSymbol.data[0].Add(parsie.tokenArr[0].lexeme);
                parsie.Update();
                if (parsie.tokenArr[0].thisType == TokenType.ARRAY_BEGIN)
                {
                    parsie.Update();
                    if (parsie.tokenArr[0].thisType == TokenType.ARRAY_END)
                    {
                        parsie.Update();
                        addSymbol.data[0].Insert(1, "@:");
                        if (secondPass)
                        {
                            semanticAnalyzer.intializeArr = true;
                        }
                    }
                    else
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "]");
                    }
                }
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "Keyword Type or Identifier");
            }
            addSymbol.lexeme = parsie.tokenArr[0].lexeme;

            if (!secondPass)
            {
                if (insideMethod)
                {
                    addSymbol.curOffset = curMethodOffset;
                    addSymbol.byteSize = 4;
                    curMethodOffset -= 4;
                }
                symbolHashSet.Add(addSymbol.symid, addSymbol);
            }
            // identifier portion of Variable dec
            if (parsie.tokenArr[0].thisType == TokenType.IDENTIFIER)
            {
                if (secondPass)
                {
                    semanticAnalyzer.VPush(parsie.tokenArr[0], scopeLevel);
                }
                parsie.Update();
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "identifier");
            }
            // assignment or semicolon
            if (parsie.tokenArr[0].lexeme == ";")
            {
                if (secondPass)
                {
                    semanticAnalyzer.comment = parsie.commentLine;
                    parsie.commentLine = "";
                    semanticAnalyzer.EoE(parsie.tokenArr[0].lineNum);
                }
                parsie.Update();
            }
            else if (parsie.tokenArr[0].lexeme == "=")
            {
                if (secondPass)
                {
                    semanticAnalyzer.OPush(parsie.tokenArr[0]);
                }
                parsie.Update();
                AssignmentExpression();
                if (parsie.tokenArr[0].lexeme == ";")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.comment = parsie.commentLine;
                        parsie.commentLine = "";
                        semanticAnalyzer.EoE(parsie.tokenArr[0].lineNum);
                    }
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ";");
                }
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "assignment or ;");
            }


        }
        /*
            runs syntax analysis for a statement
         */
        private void Statement()
        {
            parsie.commentLine = "";

            if (secondPass)
            {
                semanticAnalyzer.firstRowComment = true;
            }
            if (parsie.tokenArr[0].lexeme != "if")
            {
                exitIf = false;
            }
            if (parsie.tokenArr[0].thisType == TokenType.BLOCK_BEGIN)
            {
                parsie.Update();
                while (parsie.tokenArr[0].thisType != TokenType.BLOCK_END)
                {
                    Statement();
                    if (parsie.tokenArr[0].thisType == TokenType.EOF)
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "}");
                    }
                }
                parsie.Update();
            }
            // Statement Expression handling
            else if (IsExpression(parsie.tokenArr[0]))
            {
                Expression();
                if (parsie.tokenArr[0].lexeme == ";")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.comment = parsie.commentLine;
                        parsie.commentLine = "";
                        semanticAnalyzer.EoE(parsie.tokenArr[0].lineNum);
                    }
                    parsie.Update();
                }
                else
                {
                    if (IsExpressionz(parsie.tokenArr[0]))
                    {
                        Expressionz();
                        if (parsie.tokenArr[0].lexeme != ";") 
                        {
                            genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ";");
                        }
                        parsie.Update();
                    }
                    else
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ";");
                    }
                }
            }
            // Statement if
            else if (parsie.tokenArr[0].lexeme == "if")
            {
                parsie.Update();
                if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_OPEN)
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.OPush(parsie.tokenArr[0]);
                        semanticAnalyzer.ifExpression = true;
                        semanticAnalyzer.ifNestedExpression = true;
                    }
                    parsie.Update();
                    Expression();
                    if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_CLOSE)
                    {
                        parsie.Update();
                    }
                    else
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ")");
                    }

                    if (secondPass)
                    {
                        semanticAnalyzer.comment = parsie.commentLine;
                        semanticAnalyzer.IfCheck(parsie.tokenArr[0].lineNum);
                        semanticAnalyzer.ifExpression = false;
                        semanticAnalyzer.ifNestedExpression = false;
                    }
                    // Enter if
                    bool backPatchIf = false;
                    if (exitIf == true)
                    {
                        backPatchIf = true;
                    }
                    Statement();
                    if (secondPass)
                    {
                        quad.labelNext = true;
                    }
                    // optional else
                    if (parsie.tokenArr[0].lexeme == "else")
                    {
                        if (secondPass)
                        {                            
                            quad.labelNext = false;
                            string prevLabel = curLabel;
                            curLabel = "SKIPELSE" + quad.labelCounter;
                            if (backPatchIf)
                            {
                                quad.BackPatch(prevLabel, curLabel); // Back patch for chaing if else statements
                            }
                            quad.AddRow("", "JMP", curLabel, "", "", "");
                            quad.labelCounter++;
                            quad.labelNext = true;
                        }
                        parsie.Update();
                        exitIf = true;
                        Statement();
                        if (secondPass)
                        {
                            quad.labelStack.Push(curLabel);
                            quad.labelNext = true;
                        }
                    }
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "(");
                }
            }
            // statement while
            else if (parsie.tokenArr[0].lexeme == "while")
            {
                parsie.Update();
                if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_OPEN)
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.OPush(parsie.tokenArr[0]);
                    }
                    parsie.Update();
                    Expression();
                    if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_CLOSE)
                    {
                        parsie.Update();
                    }
                    else
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ")");
                    }

                    if (secondPass)
                    {
                        semanticAnalyzer.comment = parsie.commentLine;
                        semanticAnalyzer.WhileCheck(parsie.tokenArr[0].lineNum);
                        semanticAnalyzer.firstRowComment = true;
                    }

                    Statement();

                    if (secondPass)
                    {
                        if (quad.labelNext == true)
                        {
                            // this should only happen if we have a nested while with NO statements after the while
                            quad.labelNext = false;
                            string sLab = quad.labelStack.Pop();
                            quad.AddRow(sLab, "JMP", quad.labelStack.Pop(), "", "", "");
                        }
                        else
                        {
                            quad.AddRow("", "JMP", quad.labelStack.Pop(), "", "", "");
                        }
                        quad.labelNext = true; // force next line to have endWhile
                    }
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "(");
                }
            }
            // Statment return
            else if (parsie.tokenArr[0].lexeme == "return")
            {
                parsie.Update();
                if (secondPass && parsie.tokenArr[0].lexeme == ";")
                {
                    // if we have return; the type is void so we don't need to push to the stack
                }
                else if (secondPass && parsie.tokenArr[1].lexeme != ";" && parsie.tokenArr[1].lexeme != "=")
                {
                    semanticAnalyzer.RetPush(scopeString()); // puts a return on the stack to pretend to be i = (return expression), it will have the same type as the containing method
                    semanticAnalyzer.OPush(new Token("=", TokenType.ASSIGNMENT_OPERATOR, parsie.tokenArr[0].lineNum));
                }
                else if (secondPass && parsie.tokenArr[1].lexeme == ";")
                {
                    semanticAnalyzer.RetPush(scopeString()); // puts a return on the stack to pretend to be i = (return expression), it will have the same type as the containing method
                    semanticAnalyzer.OPush(new Token("=", TokenType.ASSIGNMENT_OPERATOR, parsie.tokenArr[0].lineNum));
                }
                // Optional return expression
                if (IsExpression(parsie.tokenArr[0]) || (parsie.tokenArr[0].lexeme == "-" && IsExpression(parsie.tokenArr[1])))
                {
                    Expression();
                }

                if (parsie.tokenArr[0].lexeme == ";")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.comment = parsie.commentLine;
                        parsie.commentLine = "";
                        semanticAnalyzer.EoE(parsie.tokenArr[0].lineNum);
                    }
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ";");
                }

            }
            // Statement cout
            else if (parsie.tokenArr[0].lexeme == "cout")
            {
                if (secondPass)
                {
                    semanticAnalyzer.IPush(parsie.tokenArr[0], scopeLevel, parsie.tokenArr[1]);
                }
                parsie.Update();
                if (parsie.tokenArr[0].lexeme == "<" && parsie.tokenArr[1].lexeme == "<")
                {
                    parsie.Update();
                    parsie.Update();
                    if (secondPass)
                    {
                        // push an equals
                        Token temp = new Token("=", TokenType.ASSIGNMENT_OPERATOR, parsie.tokenArr[0].lineNum);
                        semanticAnalyzer.OPush(temp);
                    }
                    Expression();
                }
                else
                {
                    if (parsie.tokenArr[1].lexeme == ">")
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme + parsie.tokenArr[1].lexeme, "<<");
                    }
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "<<");
                }

                if (parsie.tokenArr[0].lexeme == ";")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.comment = parsie.commentLine;
                        parsie.commentLine = "";
                        semanticAnalyzer.COut(parsie.tokenArr[0].lineNum);
                    }
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ";");
                }
            }
            // Statement cin
            else if (parsie.tokenArr[0].lexeme == "cin")
            {
                parsie.Update();
                if (parsie.tokenArr[0].lexeme == ">" && parsie.tokenArr[1].lexeme == ">")
                {
                    parsie.Update();
                    parsie.Update();
                    Expression();
                }
                else
                {

                    if (parsie.tokenArr[1].lexeme == "<")
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme + parsie.tokenArr[1].lexeme, ">>");
                    }
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ">> ");
                }

                if (parsie.tokenArr[0].lexeme == ";")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.comment = parsie.commentLine;
                        parsie.commentLine = "";
                        semanticAnalyzer.CIn(parsie.tokenArr[0].lineNum);
                    }
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ";");
                }
            }
            // Statement switch
            else if (parsie.tokenArr[0].lexeme == "switch")
            {
                parsie.Update();
                if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_OPEN)
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.OPush(parsie.tokenArr[0]);
                    }
                    parsie.Update();
                    Expression();
                    if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_CLOSE)
                    {
                        parsie.Update();
                        if (secondPass)
                        {
                            // switch is not implemented, at this point just pop the previous operator and enter case blocks if implemented add the switch expression result to the stack and compare against it in each case block
                            semanticAnalyzer.CloseSwitch();

                        }
                    }
                    else
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ")");
                    }
                    caseBlock();

                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "(");
                }
            }
            // Statement Break
            else if (parsie.tokenArr[0].lexeme == "break")
            {
                parsie.Update();
                if (parsie.tokenArr[0].lexeme == ";")
                {
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ";");
                }
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "Statement");
            }
        }
        /*
            Handle case blocks
         */
        private void caseBlock()
        {
            if (parsie.tokenArr[0].thisType == TokenType.BLOCK_BEGIN)
            {
                parsie.Update();
                while (parsie.tokenArr[0].lexeme != "default")
                {
                    CaseLabel();
                }
                parsie.Update();
                if (parsie.tokenArr[0].lexeme == ":")
                {
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ":");
                }
                Statement();
                if (parsie.tokenArr[0].thisType == TokenType.BLOCK_END)
                {
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "}");
                }
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "{");
            }
        }
        /*
            Handle Case labels
         */
        private void CaseLabel()
        {
            if (parsie.tokenArr[0].lexeme == "case")
            {
                parsie.Update();
                Literal();
                if (parsie.tokenArr[0].lexeme == ":")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.comment = parsie.commentLine;
                        semanticAnalyzer.EoE(parsie.tokenArr[0].lineNum);
                    }
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ":");
                }
                Statement();
            }
            else if (parsie.tokenArr[0].thisType == TokenType.BLOCK_END)
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "default");
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "case default");
            }
        }
        /*
            Handle Literals
         */
        private void Literal()
        {
            if (parsie.tokenArr[0].lexeme == "+" || parsie.tokenArr[0].lexeme == "-")
            {
                bool negativeFlag = false;
                if (parsie.tokenArr[0].lexeme == "-")
                {
                    negativeFlag = true;
                }
                parsie.Update();
                if (parsie.tokenArr[0].thisType == TokenType.NUMBER)
                {
                    bool existingLiteral = false;
                    foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
                    {
                        if (negativeFlag)
                        {

                            if (s.Value.lexeme == "-" + parsie.tokenArr[0].lexeme)
                            {
                                existingLiteral = true;
                                break;
                            }
                        }
                        else
                        {
                            if (s.Value.lexeme == parsie.tokenArr[0].lexeme)
                            {
                                existingLiteral = true;
                                break;
                            }
                        }
                    }

                    if (!existingLiteral)
                    {
                        if (negativeFlag)
                        {
                            addSymbol = new Symbol("g", "N" + symbolCounter, "-" + parsie.tokenArr[0].lexeme, "ilit");
                        }
                        else
                        {
                            addSymbol = new Symbol("g", "N" + symbolCounter, parsie.tokenArr[0].lexeme, "ilit");
                        }
                        addSymbol.data[0].Add("type: ");
                        addSymbol.data[0].Add("int");
                        addSymbol.data[1].Add("accessMod: ");
                        addSymbol.data[1].Add("public");
                        if (!secondPass)
                        {
                            addSymbol.byteSize = 4;
                            symbolHashSet.Add(addSymbol.symid, addSymbol);
                            symbolCounter++;
                        }
                        else
                        {
                            if (negativeFlag)
                            {
                                parsie.tokenArr[0].lexeme.Insert(0, "-");
                                semanticAnalyzer.LPush(parsie.tokenArr[0], scopeLevel);
                            }
                            else
                            {
                                semanticAnalyzer.LPush(parsie.tokenArr[0], scopeLevel);
                            }
                        }
                    }
                    if (secondPass)
                    {
                        if (negativeFlag)
                        {
                            parsie.tokenArr[0].lexeme = parsie.tokenArr[0].lexeme.Insert(0, "-");
                            semanticAnalyzer.LPush(parsie.tokenArr[0], scopeLevel);
                        }
                        else
                        {
                            semanticAnalyzer.LPush(parsie.tokenArr[0], scopeLevel);
                        }
                    }
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "number");
                }
            }
            else if (parsie.tokenArr[0].thisType == TokenType.NUMBER)
            {
                bool existingLiteral = false;
                foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
                {
                    if (s.Value.lexeme == parsie.tokenArr[0].lexeme)
                    {
                        existingLiteral = true;
                        break;
                    }
                }

                if (!existingLiteral)
                {
                    addSymbol = new Symbol("g", "N" + symbolCounter, parsie.tokenArr[0].lexeme, "ilit");
                    addSymbol.byteSize = 4;
                    addSymbol.data[0].Add("type: ");
                    addSymbol.data[0].Add("int");
                    addSymbol.data[1].Add("accessMod: ");
                    addSymbol.data[1].Add("public");

                    if (!secondPass)
                    {
                        symbolHashSet.Add(addSymbol.symid, addSymbol);
                        symbolCounter++;
                    }
                }
                if (secondPass)
                {
                    // MAYBE PUSH HERE
                    semanticAnalyzer.LPush(parsie.tokenArr[0], scopeLevel);
                }
                parsie.Update();
            }
            else if (parsie.tokenArr[0].thisType == TokenType.CHARACTER)
            {
                bool existingLiteral = false;
                foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
                {
                    if (s.Value.lexeme == parsie.tokenArr[0].lexeme)
                    {
                        existingLiteral = true;
                        break;
                    }
                }
                if (!existingLiteral)
                {
                    addSymbol = new Symbol("g", "H" + symbolCounter, parsie.tokenArr[0].lexeme, "clit");
                    addSymbol.byteSize = 1;
                    addSymbol.data[0].Add("type: ");
                    addSymbol.data[0].Add("char");
                    addSymbol.data[1].Add("accessMod: ");
                    addSymbol.data[1].Add("public");

                    if (!secondPass)
                    {
                        symbolHashSet.Add(addSymbol.symid, addSymbol);
                        symbolCounter++;
                    }
                }
                if (secondPass)
                {
                    semanticAnalyzer.LPush(parsie.tokenArr[0], scopeLevel);
                }
                parsie.Update();
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "literal");
            }

        }
        /*
            checks if the lexeme qualifies as an expressionz
         */
        public bool IsStatementStart(Token lexemeIn)
        {
            if (IsExpression(lexemeIn) || lexemeIn.thisType == TokenType.BLOCK_BEGIN || lexemeIn.lexeme == "if" || lexemeIn.lexeme == "while" || lexemeIn.lexeme == "return" || lexemeIn.lexeme == "cout" || lexemeIn.lexeme == "cin" || lexemeIn.lexeme == "switch" || lexemeIn.lexeme == "break")
            {
                return true;
            }
            return false;
        }
        /*
            checks if the lexeme qualifies as an expression
         */
        public bool IsExpression(Token lexemeIn)
        {
            if ((lexemeIn.lexeme == "+" && parsie.tokenArr[1].thisType == TokenType.NUMBER) || (lexemeIn.lexeme == "-" && parsie.tokenArr[1].thisType == TokenType.NUMBER) || lexemeIn.lexeme == "true" || lexemeIn.lexeme == "false" || lexemeIn.lexeme == "null" || lexemeIn.lexeme == "this" || lexemeIn.thisType.Equals(TokenType.IDENTIFIER) || lexemeIn.thisType.Equals(TokenType.NUMBER) || lexemeIn.thisType.Equals(TokenType.PARENTHESES_OPEN) || lexemeIn.thisType.Equals(TokenType.CHARACTER))
            {
                return true;
            }
            return false;
        }
        /*
            checks if the lexeme qualifies as an expressionz
         */
        public bool IsExpressionz(Token lexemeIn)
        {
            if (lexemeIn.lexeme == "=" || lexemeIn.lexeme == "+" || lexemeIn.lexeme == "-" || lexemeIn.lexeme == "/" || lexemeIn.lexeme == "*" || lexemeIn.lexeme == "<=" || lexemeIn.lexeme == ">=" || lexemeIn.lexeme == "!="
                || lexemeIn.lexeme == ">" || lexemeIn.lexeme == "<" || lexemeIn.lexeme == "==" || lexemeIn.lexeme == "and" || lexemeIn.lexeme == "or")
            {
                return true;
            }
            return false;
        }
        /*
            checks if the lexeme qualifies as a type
         */
        public bool TypeExists(Token lexemeIn)
        {
            if (lexemeIn.thisType == TokenType.KEYWORDS)
            {
                if (lexemeIn.lexeme == "int" || lexemeIn.lexeme == "char" || lexemeIn.lexeme == "bool" || lexemeIn.lexeme == "void" || lexemeIn.lexeme == "sym")
                {
                    return true;
                }
            }
            else if (lexemeIn.thisType == TokenType.IDENTIFIER)
            {
                foreach (string s in classes)
                {
                    if (s.Equals(lexemeIn.lexeme))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {

            }

            return false;
        }
        /*
            Error Generation
         */
        public void genSAMClassesDup(int curLine, string found)
        {
            Console.WriteLine(curLine + ": Duplicate class " + found);
            System.Environment.Exit(-1);
        }
        public void genSAMConst(int curLine, string found, string expectation)
        {
            Console.WriteLine(curLine + ": Constructor " + found + " must match class name " + expectation);
            System.Environment.Exit(-1);
        }
        public void genError(int curLine, string found, string expectation)
        {
            Console.WriteLine(curLine + ": Found " + found + " expecting " + expectation);
            System.Environment.Exit(-1);
        }

        public void genErrorTExist(int curLine, string found)
        {
            Console.WriteLine(curLine + ": Type " + found + " not defined");
            System.Environment.Exit(-1);
        }

        public void genSAMErrorDupConst(int curLine, string found)
        {
            Console.WriteLine(curLine + ": Duplicate Constructor " + found);
            System.Environment.Exit(-1);
        }

        public void genErrorSAMType(int curLine, string found)
        {
            Console.WriteLine(curLine + ": Type " + found + " not defined");
            System.Environment.Exit(-1);
        }
        /*
            Syntax Analysis Expression Waterfalling
         */
        private void Expression()
        {
            if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_OPEN)
            {
                if (secondPass)
                {
                    semanticAnalyzer.OPush(parsie.tokenArr[0]);
                    semanticAnalyzer.BALSARpush();
                }
                parsie.Update();
                Expression();
                if (secondPass)
                {
                    semanticAnalyzer.ifExpression = false;
                }
                if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_CLOSE)
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.EALSARpush(parsie.tokenArr[0].lineNum);
                    }
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ")");
                }
                // option Expressionz follow up
                if (IsExpressionz(parsie.tokenArr[0]))
                {
                    Expressionz();
                }
            }
            else if (parsie.tokenArr[0].thisType == TokenType.IDENTIFIER)
            {
                // TCheck here
                if (secondPass)
                {
                    if (parsie.tokenArr[1].lexeme != "(")
                    {
                        semanticAnalyzer.notMethod = true;
                    }
                    else
                    {
                        semanticAnalyzer.notMethod = false;
                        semanticAnalyzer.methodCall = true;
                        if (semanticAnalyzer.stack.Count == 0)
                        {
                            semanticAnalyzer.LPush(new Token("this", TokenType.KEYWORDS, parsie.tokenArr[0].lineNum), scopeLevel);
                        }
                        argList = true;
                    }
                    semanticAnalyzer.IPush(parsie.tokenArr[0], scopeLevel, parsie.tokenArr[1]);
                }
                parsie.Update();
                // optional expressionz
                if (IsExpressionz(parsie.tokenArr[0]))
                {
                    Expressionz();
                }
                // optional fn arr member
                else if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_OPEN)
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.methodCall = true;
                        semanticAnalyzer.OPush(parsie.tokenArr[0]);
                        semanticAnalyzer.BALSARpush();
                    }
                    parsie.Update();
                    if (parsie.tokenArr[0].thisType != TokenType.PARENTHESES_CLOSE)
                    {
                        while (IsExpression(parsie.tokenArr[0]))
                        {
                            if (secondPass)
                            {
                                semanticAnalyzer.getArgs = true;
                            }
                            ArgumentList(); 
                            if (secondPass)
                            {
                                semanticAnalyzer.getArgs = false;
                            }
                        }
                        if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_CLOSE)
                        {
                            if (secondPass)
                            {
                                if (argList)
                                {
                                    semanticAnalyzer.EAL(parsie.tokenArr[0].lineNum);
                                    semanticAnalyzer.methodCall = false;
                                }
                                else
                                {
                                    semanticAnalyzer.EALSARpush(parsie.tokenArr[0].lineNum);
                                    semanticAnalyzer.methodCall = false;
                                }
                            }
                            parsie.Update();
                            if (IsExpressionz(parsie.tokenArr[0]))
                            {
                                Expressionz();
                            }
                        }
                        else
                        {
                            genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ")");
                        }
                    }
                    else
                    {
                        if (secondPass)
                        {
                            semanticAnalyzer.EALSARpush(parsie.tokenArr[0].lineNum);
                            semanticAnalyzer.methodCall = false;
                        }
                        parsie.Update();
                    }
                }
                else if (parsie.tokenArr[0].thisType == TokenType.ARRAY_BEGIN)
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.OPush(parsie.tokenArr[0]);
                    }
                    parsie.Update();
                    Expression();
                    if (parsie.tokenArr[0].thisType == TokenType.ARRAY_END)
                    {
                        if (secondPass)
                        {
                            if (parsie.tokenArr[1].lexeme != ";")
                            {
                                semanticAnalyzer.firstRowComment = true;
                            }
                            semanticAnalyzer.endArr(scopeString());
                            if (semanticAnalyzer.tempVarOffset < curMethodOffset)
                            {
                                curMethodOffset = semanticAnalyzer.tempVarOffset;
                            }
                        }
                        parsie.Update();
                        if (parsie.tokenArr[0].lexeme == ".")
                        {
                            parsie.Update();
                            MemberReference();
                        }
                    }
                    else
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "]");
                    }
                    // Check for optional expresisonz
                    if (IsExpressionz(parsie.tokenArr[0]))
                    {
                        Expressionz();
                    }
                }
                else if (parsie.tokenArr[0].lexeme == ".")
                {
                    parsie.Update();
                    MemberReference();
                }
            }
            else if (parsie.tokenArr[0].thisType == TokenType.KEYWORDS)
            {
                if (parsie.tokenArr[0].lexeme == "true")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.LPush(parsie.tokenArr[0], scopeLevel);
                    }
                    parsie.Update();
                    if (IsExpressionz(parsie.tokenArr[0]))
                    {
                        Expressionz();
                    }

                }
                else if (parsie.tokenArr[0].lexeme == "false")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.LPush(parsie.tokenArr[0], scopeLevel);
                    }
                    parsie.Update();
                    if (IsExpressionz(parsie.tokenArr[0]))
                    {
                        Expressionz();
                    }
                }
                else if (parsie.tokenArr[0].lexeme == "null")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.LPush(parsie.tokenArr[0], scopeLevel);
                    }
                    parsie.Update();
                    if (IsExpressionz(parsie.tokenArr[0]))
                    {
                        Expressionz();
                    }
                }
                else if (parsie.tokenArr[0].lexeme == "this")
                {
                    thisMemberFlag = true;
                    if (secondPass)
                    {
                        semanticAnalyzer.LPush(parsie.tokenArr[0], scopeLevel);
                    }
                    parsie.Update();
                    if (IsExpressionz(parsie.tokenArr[0]))
                    {
                        Expressionz();
                    }
                    else if (parsie.tokenArr[0].lexeme == ".")
                    {
                        parsie.Update();
                        MemberReference();
                        // Handle member referces
                        if (IsExpressionz(parsie.tokenArr[0]))
                        {
                            Expressionz();
                        }
                    }
                    thisMemberFlag = false;
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "Expression Keyword");
                }
            }
            else if (parsie.tokenArr[0].thisType == TokenType.NUMBER)
            {
                Literal();
                if (IsExpressionz(parsie.tokenArr[0]))
                {
                    Expressionz();
                }
            }
            else if (parsie.tokenArr[0].thisType == TokenType.CHARACTER)
            {
                Literal();
                if (IsExpressionz(parsie.tokenArr[0]))
                {
                    Expressionz();
                }
            }
            else if (parsie.tokenArr[0].lexeme == "-" || parsie.tokenArr[0].lexeme == "+")
            {
                Literal();
                if (IsExpressionz(parsie.tokenArr[0]))
                {
                    Expressionz();
                }
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "Expression");
            }
        }

        private void Expressionz()
        {
            // Handle assignments
            if (parsie.tokenArr[0].thisType == TokenType.ASSIGNMENT_OPERATOR)
            {
                if (secondPass)
                {
                    semanticAnalyzer.OPush(parsie.tokenArr[0]);
                }
                parsie.Update();
                AssignmentExpression();
            }
            // Handle Expressionz keywords
            else if (parsie.tokenArr[0].thisType == TokenType.KEYWORDS)
            {
                if (parsie.tokenArr[0].lexeme == "and")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.OPush(parsie.tokenArr[0]);
                    }
                    parsie.Update();
                    Expression();

                }
                else if (parsie.tokenArr[0].lexeme == "or")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.OPush(parsie.tokenArr[0]);
                    }
                    parsie.Update();
                    Expression();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ": and, or");
                }
            }
            // Handle Relational Operators
            else if (parsie.tokenArr[0].thisType == TokenType.RELATIONAL_OPERATORS)
            {
                if (parsie.tokenArr[0].lexeme == "<" || parsie.tokenArr[0].lexeme == "<=" || parsie.tokenArr[0].lexeme == ">" || parsie.tokenArr[0].lexeme == ">=" || parsie.tokenArr[0].lexeme == "==" || parsie.tokenArr[0].lexeme == "!=")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.OPush(parsie.tokenArr[0]);
                    }
                    parsie.Update();
                    Expression();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "relational operator");
                }
            }
            // Handle Mathematical Expressions
            else if (parsie.tokenArr[0].thisType == TokenType.MATH_OPERATORS)
            {
                if (parsie.tokenArr[0].lexeme == "+" || parsie.tokenArr[0].lexeme == "-" || parsie.tokenArr[0].lexeme == "*" || parsie.tokenArr[0].lexeme == "/")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.OPush(parsie.tokenArr[0]);
                    }
                    parsie.Update();
                    Expression();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "mathematical operator");
                }
            }
            // Handle Errors'
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "Expressionz");
            }
        }
        /*
            Recursive waterfall for assignment expressions
         */
        private void AssignmentExpression()
        {
            if (IsExpression(parsie.tokenArr[0]))
            {
                Expression();
            }
            else if (parsie.tokenArr[0].thisType == TokenType.KEYWORDS)
            {
                if (parsie.tokenArr[0].lexeme == "new")
                {
                    parsie.Update();
                    if (secondPass && TypeExists(parsie.tokenArr[0]))
                    {
                        semanticAnalyzer.newFlag = true;
                        semanticAnalyzer.TPush(parsie.tokenArr[0].lexeme);
                        string newType = parsie.tokenArr[0].lexeme;
                        parsie.Update();
                        NewDecaration(newType);
                    }
                    else if (!secondPass)
                    {
                        string newType = parsie.tokenArr[0].lexeme;
                        parsie.Update();
                        NewDecaration(newType);
                    }
                    else
                    {
                        semanticAnalyzer.newFlag = true;
                        semanticAnalyzer.TPush(parsie.tokenArr[0].lexeme);
                        string newType = parsie.tokenArr[0].lexeme;
                        parsie.Update();
                        NewDecaration(newType);
                    }
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "new, identifier");
                }
            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "expression, new");
            }
        }
        /*
            Recursive waterfall for Argument_List
         */
        private void ArgumentList()
        {
            while (true)
            {
                if (IsExpression(parsie.tokenArr[0]))
                {
                    Expression();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "expression");
                }
                if (parsie.tokenArr[0].lexeme == ",")
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.CommaAction();
                    }
                    parsie.Update();
                }
                else
                {
                    break;
                }
            }
        }
        /*
            Recursive waterfall for type expressions
         */
        private void NewDecaration(string newType)
        {
            if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_OPEN)
            {
                DataRow commentLine = null;
                if (secondPass)
                {
                    addSymbol = new Symbol(scopeString(), "T" + semanticAnalyzer.tempVarCounter, newType, "lvar");


                    byteSize = 4;
                    semanticAnalyzer.tempVarCounter++;
                    addSymbol.byteSize = byteSize;
                    addSymbol.data[0].Add("type: ");
                    addSymbol.data[0].Add(newType);
                    addSymbol.data[1].Add("accessMod: ");
                    addSymbol.data[1].Add("public");
                    symbolHashSet.Add(addSymbol.symid, addSymbol);
                    if (insideMethod)
                    {
                        addSymbol.curOffset = curMethodOffset;
                        curMethodOffset -= 4;
                        semanticAnalyzer.tempVarOffset = curMethodOffset;
                    }
                    else
                    {
                        addSymbol.curOffset = curClassOffset;
                        curClassOffset += 4;
                        semanticAnalyzer.tempVarOffset = curMethodOffset;
                    }
                    quad.AddRow("", "NEWI", addSymbol.symid, "" + getClassSize(newType), "", parsie.commentLine); // We need to malloc here with a temp to store this temp will be the address of the object in heap
                    commentLine = quad.GetBotRow();
                    newType = FindConstbyLexeme(newType);
                    quad.AddRow("", "FRAME", newType, addSymbol.symid, "", "");
                    semanticAnalyzer.rowStored = null;
                    semanticAnalyzer.OPush(parsie.tokenArr[0]);
                    semanticAnalyzer.BALSARpush();
                }
                parsie.Update();
                if (parsie.tokenArr[0].thisType != TokenType.PARENTHESES_CLOSE)
                {
                    ArgumentList();
                }

                if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_CLOSE)
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.EAL(parsie.tokenArr[0].lineNum);
                    }
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ")");
                }
                if (secondPass)
                {
                    commentLine[5] = parsie.commentLine;
                    parsie.commentLine = "";
                }
            }
            else if (parsie.tokenArr[0].thisType == TokenType.ARRAY_BEGIN)
            {
                if (secondPass)
                {
                    semanticAnalyzer.OPush(parsie.tokenArr[0]);
                }

                parsie.Update();
                Expression();
                if (parsie.tokenArr[0].thisType == TokenType.ARRAY_END)
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.comment = parsie.commentLine + "]";
                        parsie.commentLine = "";
                        semanticAnalyzer.endArr(scopeString());
                    }
                    parsie.Update();
                }
                else
                {
                    genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "]");
                }

            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "(, [");
            }
        }
        /*
            Private member reference
         */
        private void MemberReference()
        {
            string callID = "";
            string[] row = null;
            if (parsie.tokenArr[0].thisType == TokenType.IDENTIFIER)
            {
                // member ref push
                if (secondPass)
                {
                    if (parsie.tokenArr[1].thisType.Equals(TokenType.ARRAY_BEGIN))
                    {
                        semanticAnalyzer.checkie = 2;
                    }
                    else if (parsie.tokenArr[1].thisType.Equals(TokenType.PARENTHESES_OPEN))
                    {
                        semanticAnalyzer.checkie = 1;
                    }
                    else
                    {
                        semanticAnalyzer.checkie = 0;
                    }
                    semanticAnalyzer.incomingMemberRef = true;
                    semanticAnalyzer.IPush(parsie.tokenArr[0], scopeLevel, parsie.tokenArr[1]);

                    if (semanticAnalyzer.checkie == 1 && (string)quad.GetBotRow()[1] == "FRAME")
                    {
                        callID = (string)quad.GetBotRow()[2];
                        row = quad.RemoveBotRow();
                        string label = row[0].ToString();
                        if (quad.GetBotRowArr()[1] == "JMP")
                        {
                            quad.labelNext = true;
                            quad.labelStack.Push(label);
                            row[0] = "";
                        }
                    }
                    else if (semanticAnalyzer.checkie == 1)
                    {

                        quad.AddRow("", "FRAME", semanticAnalyzer.stack.Peek().symbol.symid, "this", "", "");
                        if (thisMemberFlag)
                        {
                            SARbase plate = semanticAnalyzer.stack.Pop();
                            SARinstance thisPlate = new SARinstance("this", new Symbol(scopeString(), "this", "this", "this"));
                            semanticAnalyzer.stack.Push(thisPlate);
                            semanticAnalyzer.stack.Push(plate);
                        }
                    }

                }
                parsie.Update();
                if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_OPEN)
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.OPush(parsie.tokenArr[0]);
                        semanticAnalyzer.BALSARpush();
                    }
                    parsie.Update();
                    if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_CLOSE)
                    {
                        parsie.Update();
                        if (secondPass)
                        {
                            if (row != null)
                            {
                                quad.AddRow(row[0].ToString(), row[1].ToString(), row[2].ToString(), row[3].ToString(), row[4].ToString(), row[5].ToString());
                            }
                            semanticAnalyzer.rowStored = null;
                            semanticAnalyzer.incomingMemberRef = true;
                            semanticAnalyzer.EAL(parsie.tokenArr[0].lineNum); // pushes from the paramters to thew quad should be here
                        }
                    }
                    else
                    {
                        if (secondPass)
                        {
                            semanticAnalyzer.inArgList = true;
                        }
                        ArgumentList();
                        if (secondPass)
                        {
                            semanticAnalyzer.inArgList = false;
                        }
                        if (parsie.tokenArr[0].thisType == TokenType.PARENTHESES_CLOSE)
                        {
                            if (secondPass)
                            {
                                semanticAnalyzer.rowStored = row;
                                semanticAnalyzer.comment = parsie.commentLine + ")";
                                semanticAnalyzer.incomingMemberRef = true;
                                semanticAnalyzer.EAL(parsie.tokenArr[0].lineNum); // pushes from the paramters to thew quad should be here
                            }
                            parsie.Update();
                            if (IsExpressionz(parsie.tokenArr[0]))
                            {
                                Expressionz();
                            }
                        }
                        else
                        {
                            genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, ")");
                        }
                    }
                }
                else if (parsie.tokenArr[0].lexeme == ".")
                {
                    parsie.Update();
                    MemberReference();
                }
                else if (IsExpressionz(parsie.tokenArr[0]))
                {
                    Expressionz();
                }
                else if (parsie.tokenArr[0].thisType.Equals(TokenType.ARRAY_BEGIN))
                {
                    if (secondPass)
                    {
                        semanticAnalyzer.OPush(parsie.tokenArr[0]);
                    }

                    parsie.Update();
                    Expression();
                    if (parsie.tokenArr[0].thisType == TokenType.ARRAY_END)
                    {
                        if (secondPass)
                        {
                            semanticAnalyzer.memberArr = true;
                            semanticAnalyzer.curLine = parsie.tokenArr[0].lineNum;
                            semanticAnalyzer.endArr(scopeString());
                        }
                        parsie.Update();
                    }
                    else
                    {
                        genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "]");
                    }
                }

                if (parsie.tokenArr[0].lexeme == ".")
                {
                    // Nested member referneces
                    parsie.Update();
                    MemberReference();
                }
                if (IsExpressionz(parsie.tokenArr[0]))
                {
                    Expressionz();
                }

            }
            else
            {
                genError(parsie.tokenArr[0].lineNum, parsie.tokenArr[0].lexeme, "identifer");
            }
        }

        public string FindMethodSymId(string scope, string lexeme)
        {
            string retStr = "";

            foreach (KeyValuePair<String, Symbol> s in symbolHashSet)
            {
                if (s.Value.scope == scope && s.Value.lexeme == lexeme)
                {
                    return s.Value.symid;
                }
            }
            return retStr;
        }
        public string FindConstbyLexeme(string lexeme)
        {
            string retStr = "";

            foreach (KeyValuePair<String, Symbol> s in symbolHashSet)
            {
                if (s.Value.symid[0] == 'X' && s.Value.lexeme == lexeme)
                {
                    return s.Value.symid;
                }
            }
            return retStr;
        }
        public int getClassSize(string className)
        {
            foreach (KeyValuePair<String, Symbol> s in symbolHashSet)
            {
                if (s.Value.scope == "g" && s.Value.lexeme == className)
                {
                    return s.Value.byteSize;
                }
            }
            return 0;
        }
        /*
         * 
         * 
         * 
         * 
         * 
         * 
         *   private NESTED ETparser class for syntax analysis
         *
         *
         *
         *
         *
         */


        private class ETparser
        {

            public bool EOFflag = false;
            public Token[] tokenArr = new Token[3];
            public StreamReader ETreader = new StreamReader("ET1.txt");
            private int lineNumMain = 0;
            public string commentLine = "";
            private string tokenLine;
            public ETparser()
            {

            }
            /*
                Updates our current token to the next one
             */
            public void Update()
            {
                if (tokenArr[0] != null)
                {
                    if (commentLine.Length == 0)
                    {
                        commentLine += tokenArr[0].lineNum + ") ";
                    }
                    if (tokenArr[0].thisType == TokenType.KEYWORDS)
                    {
                        commentLine += " " + tokenArr[0].lexeme + " ";
                    }
                    else if (tokenArr[0].thisType == TokenType.IDENTIFIER)
                    {
                        commentLine += " " + tokenArr[0].lexeme + " ";
                    }
                    else
                    {
                        commentLine += tokenArr[0].lexeme;
                    }
                }
                tokenArr[0] = tokenArr[1];
                tokenArr[1] = tokenArr[2];
                tokenArr[2] = readNextToken();

                while (tokenArr[0] == null)
                {
                    this.Update();
                }
            }

            private Token readNextToken()
            {
                // Read in an EOF token if and only if we have reached the end of a file
                if (EOFflag)
                {
                    return new Token("", TokenType.EOF, lineNumMain); ;
                }
                tokenLine = ETreader.ReadLine();

                string[] tokenSplit = tokenLine.Split(' ');
                if (tokenSplit.Length > 3)
                {
                    // Passing a space
                    if (tokenSplit[1] == "'" && tokenSplit[2] == "'")
                    {
                        string[] spaceCase = new string[3];
                        spaceCase[0] = tokenSplit[0];

                        spaceCase[1] = "' '";

                        spaceCase[2] = tokenSplit[3];
                        tokenSplit = spaceCase;
                    }
                    else
                    {
                        throw new Exception("Invalid Token Format in ET1");
                    }
                }
                string lexeme = tokenSplit[1];
                int lineNum = Convert.ToInt32(tokenSplit[0]);
                lineNumMain = lineNum;
                TokenType readType;
                switch (tokenSplit[2])
                {
                    case "ASSIGNMENT_OPERATOR":
                        readType = TokenType.ASSIGNMENT_OPERATOR;
                        break;
                    case "NUMBER":
                        readType = TokenType.NUMBER;
                        break;
                    case "CLASS_DECLARATION":
                        readType = TokenType.CLASS_DECLARATION;
                        break;
                    case "CHARACTER":
                        readType = TokenType.CHARACTER;
                        break;
                    case "IDENTIFIER":
                        readType = TokenType.IDENTIFIER;
                        break;
                    case "PUNCTUATION":
                        readType = TokenType.PUNCTUATION;
                        break;
                    case "KEYWORDS":
                        readType = TokenType.KEYWORDS;
                        break;
                    case "MATH_OPERATORS":
                        readType = TokenType.MATH_OPERATORS;
                        break;
                    case "RELATIONAL_OPERATORS":
                        readType = TokenType.RELATIONAL_OPERATORS;
                        break;
                    case "ARRAY_BEGIN":
                        readType = TokenType.ARRAY_BEGIN;
                        break;
                    case "ARRAY_END":
                        readType = TokenType.ARRAY_END;
                        break;
                    case "BLOCK_BEGIN":
                        readType = TokenType.BLOCK_BEGIN;
                        break;
                    case "BLOCK_END":
                        readType = TokenType.BLOCK_END;
                        break;
                    case "PARENTHESES_OPEN":
                        readType = TokenType.PARENTHESES_OPEN;
                        break;
                    case "PARENTHESES_CLOSE":
                        readType = TokenType.PARENTHESES_CLOSE;
                        break;
                    case "IO_OPERATORS":
                        readType = TokenType.IO_OPERATORS;
                        break;
                    case "UNKNOWN":
                        readType = TokenType.UNKNOWN;
                        break;
                    case "EOF":
                        EOFflag = true;
                        readType = TokenType.EOF;
                        ETreader.Close();
                        break;
                    default:
                        throw new Exception("SYNTAX ANALYSIS --- INVALID TYPE READ: " + tokenSplit[2]);

                }

                return new Token(lexeme, readType, lineNum);
            }
        }
    }
}