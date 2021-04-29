using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
/*
    Semantic Action Stack
 */
namespace Compiler
{
    class SAS
    {
        public Stack<SARbase> stack;
        public Stack<SARbase> oStack;
        public int curLine = 0;
        public Dictionary<string, Symbol> symbolHashSet;
        public Dictionary<string, int> dupChecker = new Dictionary<string, int>();
        public int tempVarCounter = 1;
        public bool incomingMemberRef = false;
        public bool newFlag = false;
        public bool intializeArr = false;
        public bool memberArr = false;
        public bool insideConstructor = false;
        public bool notMethod = true;
        public bool methodCall = false;
        public int checkie = 0;
        public int tempVarOffset = 0;
        public QuadTable quad;
        public string comment = "";
        public string scope = "";
        public string[] rowStored;
        // COmment features
        public bool firstRowComment = true;
        public bool insideMethod = false;
        public bool inArgList = false;
        public bool ifExpression = false;
        public bool ifNestedExpression = false;
        public bool refFuncError = false;
        public bool memFuncError = false;
        private string refErrorName = "";
        public DataRow commentRow = null;
        // Type Checking
        List<string> classes;
        public SAS(Dictionary<string, Symbol> _symbolHashSet, QuadTable quadPointer, List<string> _classes)
        {
            symbolHashSet = _symbolHashSet;
            tempVarCounter = 1;
            quad = quadPointer;
            classes = _classes;
        }
        public void RExist(SymbolTable s, string lexeme)
        {

        }
        /*
            Check if the instance variable exists inside the scope of the probalm, if multiple exist throw an error
         */
        public void IPush(Token inToken, List<string> scope)
        {
            if (null == stack)
            {
                stack = new Stack<SARbase>();
            }
            int instanceCount = 0;
            string parentId = "";
            if (incomingMemberRef)
            {
                parentId = stack.Peek().symbol.symid;
            }

            int prevMostSpecific = 0;

            Symbol addie = null;
            bool addTemp = false;
            bool leavingSARref = false;
            string scopeCheck = "";
            SARinstance delayedPush = null;
            bool refAdd = false;
            bool foundRef = false;
            if (parentId == "this")
            {
                // decompose scope on this and check if member exists
                string[] decomped = stack.Peek().symbol.scope.Split(".");
                List<string> scopeClimb = new List<string>();
                foreach (string s in decomped)
                {
                    scopeClimb.Add(s);
                }
                // verify this exists!
                bool foundNearest = false;
                while (scopeClimb.Count > 1 && !foundNearest)
                {
                    scopeCheck = "";
                    for (int i = 0; i < scopeClimb.Count; i++)
                    {
                        scopeCheck += scopeClimb[i];
                        if (i < scopeClimb.Count - 1)
                        {
                            scopeCheck += ".";
                        }
                    }
                    foreach (KeyValuePair<string, Symbol> verifySym in symbolHashSet)
                    {
                        if (foundNearest)
                        {
                            break;
                        }
                        if (verifySym.Value.scope.Equals(scopeCheck) && inToken.lexeme.Equals(verifySym.Value.lexeme))
                        {
                            if (verifySym.Value.data[1][1] == "private")
                            {
                                instanceCount--;
                            }
                            instanceCount++;
                            if (verifySym.Value.symid[0] == 'M')
                            {
                                stack.Pop();
                                stack.Push(new SARinstance(verifySym.Value.symid, verifySym.Value));
                                foundNearest = true;
                            }
                            else
                            {
                                SARbase left = stack.Pop();

                                if (left is SARref)
                                {
                                    addie = new Symbol(left.symbol.scope, "T" + tempVarCounter, left.symbol.lexeme, left.symbol.kind);
                                    for (int i = 0; i < 3; i++)
                                    {
                                        foreach (string s in left.symbol.data[i])
                                        {
                                            addie.data[i].Add(s);
                                        }
                                    }
                                    addTemp = true;
                                    tempVarCounter++;
                                }
                                // It exists create SAR and push it onto the stack
                                stack.Push(new SARref(left.xId + "." + inToken.lexeme, left, new SARinstance(inToken.lexeme, verifySym.Value)));
                                foundNearest = true;
                                // add the sarRef to the symbolTable
                            }
                            break;
                        }
                    }
                    scopeClimb.RemoveAt(scopeClimb.Count - 1);
                }
                // If we don't find the symbol throw an error
                if (!foundNearest)
                {
                    genError(inToken.lineNum, "this." + inToken.lexeme + " does not exist", "Declaration for " + inToken.lexeme);
                }

                incomingMemberRef = false;
                return;
            }
            foreach (KeyValuePair<string, Symbol> symbol in symbolHashSet)
            {
                // Bug Fixing
                if (foundRef)
                {
                    foundRef = false;
                    break;
                }
                // Handle Member refs 
                if (incomingMemberRef)
                {
                    // nested SAR
                    if (stack.Peek() is SARref)
                    {
                        // Get type and find the class that matches, verify class contains the member
                        SARref topper = (SARref)stack.Peek();
                        if (symbol.Value.data[0].Count > 2)
                        {
                            string type = symbol.Value.data[0][1];
                        }
                        scopeCheck = "g." + topper.symbol.data[0][1];
                        foreach (KeyValuePair<string, Symbol> verifySym in symbolHashSet)
                        {
                            if (verifySym.Value.scope.Equals(scopeCheck) && inToken.lexeme.Equals(verifySym.Value.lexeme))
                            {

                                if (verifySym.Value.data[1][1] == "private")
                                {
                                    instanceCount--;
                                }
                                if (verifySym.Value.symid[0] == 'M')
                                {
                                    SARinstance memRef = new SARinstance(verifySym.Value.symid, verifySym.Value);
                                    if (delayedPush != null && delayedPush.xId == memRef.xId)
                                    {
                                        instanceCount--;
                                        break;
                                    }
                                    delayedPush = memRef;
                                    leavingSARref = true;
                                    // push the method up frame onto the 
                                    quad.AddRow("", "FRAME", memRef.symbol.symid, topper.symbol.symid, "", "");
                                    if (firstRowComment)
                                    {
                                        commentRow = quad.GetBotRow();
                                        firstRowComment = false;
                                    }
                                }
                                else
                                {
                                    SARbase left = stack.Pop();
                                    // TODO STACKFIX

                                    if (left is SARref)
                                    {
                                        // check if a matching lexeme and symbol are the table and use that instead
                                        addie = new Symbol(left.symbol.scope, "R" + tempVarCounter, left.symbol.lexeme + "." + inToken.lexeme, left.symbol.kind);
                                        for (int i = 0; i < 3; i++)
                                        {
                                            foreach (string s in verifySym.Value.data[i])
                                            {
                                                addie.data[i].Add(s);
                                            }
                                        }
                                        quad.AddRow("", "REF", left.symbol.symid, verifySym.Value.symid, addie.symid, comment);
                                        if (firstRowComment)
                                        {
                                            commentRow = quad.GetBotRow();
                                            firstRowComment = false;
                                        }
                                        comment = "";
                                        addTemp = true;
                                        tempVarCounter++;
                                    }
                                    // It exists create SAR and push it onto the stack
                                    stack.Push(new SARref(left.xId + "." + inToken.lexeme, left, new SARinstance(inToken.lexeme, verifySym.Value)));

                                    // add the sarRef to the symbolTable
                                }
                                break;
                            }
                        }
                        // if we could not find the member throw an error
                        if (instanceCount == 0)
                        {
                            string classLex = "" + scopeCheck;
                            classLex = classLex.Remove(0, 2);
                            if (checkie == 0)
                            {
                                genSAMErrorMemRef(inToken.lineNum, "Variable", inToken.lexeme, classLex);
                            }
                            else if (checkie == 1)
                            {
                                genSAMErrorMemRef(inToken.lineNum, "Function", inToken.lexeme, classLex);
                            }
                            else
                            {
                                genSAMErrorMemRef(inToken.lineNum, "Array", inToken.lexeme, classLex);
                            }
                        }
                        if (instanceCount > 1)
                        {
                            genError(inToken.lineNum, "duplicate " + inToken.lexeme, "single member ");
                        }

                    }
                    else if (symbol.Value.symid.Equals(parentId))
                    {
                        // Get type and find the class that matches, verify class contains the member
                        string type = symbol.Value.data[0][1];
                        scopeCheck = "g." + type;
                        foreach (KeyValuePair<string, Symbol> verifySym in symbolHashSet)
                        {
                            if (verifySym.Value.scope.Equals(scopeCheck) && inToken.lexeme.Equals(verifySym.Value.lexeme))
                            {
                                instanceCount++;
                                if (verifySym.Value.symid[0] == 'M')
                                {
                                    SARbase quadDat = stack.Peek();
                                    stack.Push(new SARinstance(verifySym.Value.symid, verifySym.Value));
                                    // Push the method Onto the quad
                                    if (verifySym.Value.data[2][1] == "private")
                                    {
                                        instanceCount--;
                                        stack.Pop();
                                    }
                                    else
                                    {
                                        // push the method up frame onto the    
                                        quad.AddRow("", "FRAME", verifySym.Key, quadDat.symbol.symid, "", "");
                                        if (firstRowComment)
                                        {
                                            commentRow = quad.GetBotRow();
                                            firstRowComment = false;
                                        }
                                    }
                                }
                                else
                                {
                                    SARbase left = stack.Pop();
                                    // It exists create SAR and push it onto the stack
                                    SARref memRef = new SARref(left.xId + "." + inToken.lexeme, left, new SARinstance(inToken.lexeme, verifySym.Value));
                                    // MEMREF SETUP HERE
                                    memRef.symbol.byteSize = 4;
                                    memRef.symbol.lexeme = memRef.xId;
                                    memRef.symbol.symid = "R" + tempVarCounter;
                                    memRef.symbol.curOffset = tempVarOffset;
                                    refAdd = true;
                                    // Verify you are inside a method here;
                                    if (insideMethod)
                                    {
                                        tempVarOffset -= 4;
                                    }
                                    else
                                    {
                                        tempVarOffset += 4;
                                    }
                                    tempVarCounter++;
                                    addie = memRef.symbol;
                                    // Push line onto quad
                                    // LIKELY WE NEED TO ADD THE SYMBOL HERE
                                    quad.AddRow("", "REF", memRef.storedData[0].symbol.symid, memRef.storedData[1].symbol.symid, addie.symid, comment);
                                    if (firstRowComment)
                                    {
                                        commentRow = quad.GetBotRow(); // sets the first operations in the Statement, this is where our comment should start
                                        firstRowComment = false;
                                    }
                                    stack.Push(memRef);
                                    foundRef = true;
                                    // add the sarRef to the symbolTable
                                }
                                break;
                            }
                        }
                        // if we could not find the member throw an error

                        if (instanceCount == 0)
                        {

                            string classLex = "" + scopeCheck;
                            classLex = classLex.Remove(0, 2);
                            if (checkie == 0)
                            {
                                genSAMErrorMemRef(inToken.lineNum, "Variable", inToken.lexeme, classLex);
                            }
                            else if (checkie == 1)
                            {
                                refErrorName = inToken.lexeme;
                                refFuncError = true;
                            }
                            else
                            {
                                genSAMErrorMemRef(inToken.lineNum, "Array", inToken.lexeme, classLex);
                            }
                        }
                        if (instanceCount > 1)
                        {
                            genError(inToken.lineNum, "duplicate " + inToken.lexeme, "single member ");

                        }                        // verify class contains current symbol (Cat.kittens should have kittens in the class for this to make a SARmemRef)
                    }

                }
                // Check if iExists
                else if (symbol.Value.lexeme.Equals(inToken.lexeme))
                {
                    int scopeLevel = 0;
                    // Verify Scope is correct
                    bool foundSym = true;
                    if (!notMethod && symbol.Value.symid[0] != 'M')
                    {
                        foundSym = false;
                        continue;
                    }
                    int index = 0;
                    // Verify valid scope
                    foreach (string symScope in symbol.Value.scope.Split("."))
                    {
                        if (index < scope.Count && symScope.Equals(scope[index]))
                        {
                            index++;
                            // g.Cat.printSum is in scope to g.Cat, if we find g.Cat.printSum for the iVar then the more specific one becomes the new stack item
                            scopeLevel++;
                            if (notMethod && symbol.Value.kind == "method")
                            {
                                foundSym = false;
                            }
                        }
                        else
                        {
                            foundSym = false;
                            break;
                        }
                    }

                    // Push onto Sar, if duplicate instance of same scope found throw error do not push classes on 

                    if (symbol.Value.kind == "class" || symbol.Value.kind == "constructor")
                    {
                        foundSym = false;
                    }

                    if (foundSym)
                    {
                        if (instanceCount == 0)
                        {
                            instanceCount++;
                            stack.Push(new SARinstance(inToken.lexeme, symbol.Value));
                            prevMostSpecific = scopeLevel;
                        }
                        else if (instanceCount == 1 && (scopeLevel > prevMostSpecific))
                        {
                            stack.Pop(); // Remove the less specific version of the same instance 
                            stack.Push(new SARinstance(inToken.lexeme, symbol.Value));
                            prevMostSpecific = scopeLevel;
                        }
                    }
                }
            }
            // If we don't find the symbol throw an error
            if (leavingSARref)
            {
                stack.Push(delayedPush);
            }
            if (instanceCount == 0 && inToken.lexeme != "cout" && !refFuncError && notMethod)
            {
                genSAMErrorIEXIST(inToken.lineNum, "variable", inToken.lexeme);
            }
            else if (instanceCount == 0 && inToken.lexeme != "cout" && !refFuncError && !notMethod)
            {
                memFuncError = true;
                refErrorName = inToken.lexeme;
            }
            if (inToken.lexeme == "cout")
            {
                stack.Push(new SARinstance(inToken.lexeme, new Symbol("g", "cout", "cout", "void")));
                Symbol cout = stack.Peek().symbol;
                cout.data[0].Add("type: ");
                cout.data[0].Add("void");
            }
            else if (addTemp && (null != addie))
            {
                addie.byteSize = 4;
                addie.curOffset = tempVarOffset;
                if (insideMethod)
                {
                    tempVarOffset -= 4;
                }
                else
                {
                    tempVarOffset += 4;
                }
                symbolHashSet.Add(addie.symid, addie);
                stack.Peek().symbol = addie;
            }
            else if (incomingMemberRef && refAdd)
            {
                symbolHashSet.Add(addie.symid, addie);
            }
            incomingMemberRef = false;
            // Frame it here 
            if (stack.Peek().symbol.symid[0] == 'M')
            {
                if (quad.GetBotRow()[1].ToString() != "FRAME")
                {
                    quad.AddRow("", "FRAME", stack.Peek().symbol.symid, "this", "", "");
                    if (firstRowComment)
                    {
                        commentRow = quad.GetBotRow();
                        firstRowComment = false;
                    }
                }
            }
        }

        public void LPush(Token inToken, List<string> scope)
        {
            if (null == stack)
            {
                stack = new Stack<SARbase>();
            }
            int instanceCount = 0;
            if (inToken.lexeme == "this")
            {
                string scopeComb = "";
                for (int i = 0; i < scope.Count; i++)
                {
                    scopeComb += scope[i];
                    if (i < scope.Count - 1)
                    {
                        scopeComb += ".";
                    }
                }

                Symbol instance = new Symbol(scopeComb, "this", "this", "this");
                instance.data[0].Add("type: ");
                instance.data[0].Add(scope[1]);
                stack.Push(new SARliteral(inToken.lexeme, instance));
                instanceCount++;
            }
            else
            {
                foreach (KeyValuePair<string, Symbol> symbol in symbolHashSet)
                {
                    // Check if lExists (literals are "global" so their scope is irrelevant)
                    if (symbol.Value.lexeme.Equals(inToken.lexeme))
                    {
                        instanceCount++;
                        bool foundSym = true;
                        int index = 0;
                        foreach (string symScope in symbol.Value.scope.Split("."))
                        {
                            if (index < scope.Count && symScope.Equals(scope[index]))
                            {
                                index++;
                            }
                            else
                            {
                                foundSym = false;
                                break;
                            }
                        }

                        // Push onto Sar, if duplicate instance of same scope found throw error

                        if (foundSym)
                        {
                            stack.Push(new SARliteral(inToken.lexeme, symbol.Value));
                        }
                    }
                }
            }

            // If we don't find the symbol throw an error
            if (instanceCount == 0)
            {
                genSAMErrorIEXIST(inToken.lineNum, "variable", inToken.lexeme);
            }
        }

        /*
             Check if the instance variable exists inside the scope of the probalm, if multiple exist throw an error
          */
        public void VPush(Token inToken, List<string> scope)
        {
            if (null == stack)
            {
                stack = new Stack<SARbase>();
            }
            int instanceCount = 0;
            foreach (KeyValuePair<string, Symbol> symbol in symbolHashSet)
            {
                // Check if iExists
                if (symbol.Value.lexeme.Equals(inToken.lexeme))
                {
                    int scopeLevel = 0;
                    int prevMostSpecific = 0;
                    // Verify Scope is correct
                    bool foundSym = true;
                    int index = 0;
                    // Verify valid scope
                    foreach (string symScope in symbol.Value.scope.Split("."))
                    {
                        if (index < scope.Count && symScope.Equals(scope[index]))
                        {
                            index++;
                            // g.Cat.printSum is in scope to g.Cat, if we find g.Cat.printSum for the iVar then the more specific one becomes the new stack item
                            scopeLevel++;
                        }
                        else
                        {
                            foundSym = false;
                            break;
                        }
                    }

                    // Push onto Sar, if duplicate variables of same scope found throw error
                    string dupCheck = "";
                    for (int i = 0; i < scope.Count; i++)
                    {
                        dupCheck += scope[i];
                        dupCheck += '.';
                    }
                    dupCheck += inToken.lexeme;

                    if (foundSym)
                    {
                        if (!dupChecker.ContainsKey(dupCheck) && scopeLevel == scope.Count)
                        {
                            instanceCount++;
                            stack.Push(new SARinstance(inToken.lexeme, symbol.Value));
                            if (dupCheck.Contains(dupCheck))
                            {
                                dupChecker.Add(dupCheck, 1);
                                break;
                            }
                            prevMostSpecific = scopeLevel;
                        }
                        else
                        {
                            if (symbol.Value.kind == "method")
                            {
                                genSAMErrorDUP(inToken.lineNum, "function", inToken.lexeme);
                            }
                            else
                            {
                                if (dupChecker.ContainsKey(dupCheck) && dupChecker[dupCheck] > 0)
                                {
                                    genSAMErrorDUP(inToken.lineNum, "variable", inToken.lexeme);
                                }
                            }
                        }
                    }
                }
            }

            // If we don't find the symbol throw an error
            if (instanceCount == 0)
            {
                genSAMErrorIEXIST(inToken.lineNum, "Variable", inToken.lexeme);
            }

            if (intializeArr)
            {
                intializeArr = false;
                SARinstance right = (SARinstance)stack.Pop();
                SAR_Type left = (SAR_Type)stack.Pop();
                if (left.xId != right.symbol.data[0][2])
                {
                    genError(inToken.lineNum, right.symbol.data[0][2], left.xId);
                }
                stack.Push(right);
            }
        }

        /*
            Push the operator onto the operator stack for the SYA    
        */
        public void OPush(Token inToken)
        {
            if (null == oStack)
            {
                oStack = new Stack<SARbase>();
            }
            // verify valid operators
            if (inToken.lexeme == "and" || inToken.lexeme == "or" || inToken.lexeme == "==" || inToken.lexeme == "!=" || inToken.lexeme == "<=" || inToken.lexeme == ">=" || inToken.lexeme == "<"
                || inToken.lexeme == ">" || inToken.lexeme == "+" || inToken.lexeme == "-" || inToken.lexeme == "*" || inToken.lexeme == "/" || inToken.lexeme == "=" || inToken.lexeme == "(" || inToken.lexeme == "[")
            {
                if (inToken.lexeme != "=" && inToken.thisType.Equals(TokenType.MATH_OPERATORS) && (oStack.Peek().token.lexeme.Equals("*") || oStack.Peek().token.lexeme.Equals("/")))
                {
                    /*
                        Order of Operations Enforced here, 
                     */
                    SARbase rightSide = stack.Pop();
                    SARbase leftSide = stack.Pop();
                    SARoperator op = (SARoperator)oStack.Pop();

                    SARtemp tempie = new SARtemp("T" + tempVarCounter, leftSide, op, rightSide);
                    // We add a new temp variable here the activation record needs SPACE for this
                    Symbol addSym = (Symbol)tempie.symbol.Clone();
                    addSym.symid = tempie.xId;
                    addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                    addSym.curOffset = tempVarOffset;
                    tempie.symbol = addSym;
                    addSym.byteSize = 4;
                    symbolHashSet.Add(addSym.symid, addSym);
                    if (insideMethod)
                    {
                        tempVarOffset -= 4;
                    }
                    else
                    {
                        tempVarOffset += 4;
                    }
                    tempVarCounter++;

                    stack.Push(tempie);
                    if (op.token.lexeme == "*")
                    {
                        quad.AddRow("", "MUL", leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        if (firstRowComment)
                        {
                            commentRow = quad.GetBotRow(); // sets the first operations in the Statement, this is where our comment should start
                            firstRowComment = false;
                        }
                        comment = "";
                    }
                    else if (op.token.lexeme == "/")
                    {
                        quad.AddRow("", "DIV", leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        if (firstRowComment)
                        {
                            commentRow = quad.GetBotRow(); // sets the first operations in the Statement, this is where our comment should start
                            firstRowComment = false;
                        }
                        comment = "";
                    }
                }
                else if (inToken.thisType.Equals(TokenType.RELATIONAL_OPERATORS))
                {
                    if (!(oStack.Count == 0))
                    {
                        while (oStack.Peek().token.thisType.Equals(TokenType.MATH_OPERATORS))
                        {
                            // if we have an expression on the left of the comparator, attempt to reduce it to a single expression.
                            SARbase rightSide = stack.Pop();
                            SARbase leftSide = stack.Pop();
                            SARoperator op = (SARoperator)oStack.Pop();

                            SARtemp tempie = new SARtemp("T" + tempVarCounter, leftSide, op, rightSide);

                            // We add a new temp variable here the activation record needs SPACE for this
                            Symbol addSym = (Symbol)tempie.symbol.Clone();
                            addSym.symid = tempie.xId;
                            addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                            addSym.curOffset = tempVarOffset;
                            addSym.byteSize = 4;
                            symbolHashSet.Add(addSym.symid, addSym);
                            if (insideMethod)
                            {
                                tempVarOffset -= 4;
                            }
                            else
                            {
                                tempVarOffset += 4;
                            }
                            tempVarCounter++;
                            stack.Push(tempie);
                            // verify comment
                            string type = "";
                            switch (op.token.lexeme)
                            {
                                case "*":
                                    type = "MUL";
                                    break;
                                case "/":
                                    type = "DIV";
                                    break;
                                case "+":
                                    type = "ADD";
                                    break;
                                case "-":
                                    type = "SUB";
                                    break;
                            }
                            quad.AddRow("", type, leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                            if (firstRowComment)
                            {
                                commentRow = quad.GetBotRow(); // sets the first operations in the Statement, this is where our comment should start
                                firstRowComment = false;
                            }
                            comment = "";
                        }
                    }
                }
                else if (inToken.thisType.Equals(TokenType.KEYWORDS))
                {
                    // Handle any post relational math
                    while (oStack.Peek().token.thisType.Equals(TokenType.MATH_OPERATORS))
                    {
                        // if we have an expression on the left of the comparator, attempt to reduce it to a single expression.
                        SARbase rightSide = stack.Pop();
                        SARbase leftSide = stack.Pop();
                        SARoperator op = (SARoperator)oStack.Pop();

                        SARtemp tempie = new SARtemp("T" + tempVarCounter, leftSide, op, rightSide);

                        // We add a new temp variable here the activation record needs SPACE for this
                        Symbol addSym = (Symbol)tempie.symbol.Clone();
                        addSym.symid = tempie.xId;
                        addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                        addSym.curOffset = tempVarOffset;
                        addSym.byteSize = 4;
                        symbolHashSet.Add(addSym.symid, addSym);
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        tempVarCounter++;

                        stack.Push(tempie);
                    }
                    if (oStack.Peek().token.thisType.Equals(TokenType.RELATIONAL_OPERATORS))
                    {
                        SARbase rightSide = stack.Pop();
                        SARbase leftSide = stack.Pop();
                        SARoperator op = (SARoperator)oStack.Pop();

                        SARtemp tempie = new SARtemp("T" + tempVarCounter, leftSide, op, rightSide);

                        // We add a new temp variable here the activation record needs SPACE for this
                        Symbol addSym = (Symbol)tempie.symbol.Clone();
                        addSym.symid = tempie.xId;
                        addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                        addSym.curOffset = tempVarOffset;
                        addSym.byteSize = 4;
                        symbolHashSet.Add(addSym.symid, addSym);
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        tempVarCounter++;

                        stack.Push(tempie);
                        // Handle Relationals
                        string eqType = "";
                        switch (op.token.lexeme)
                        {
                            case "<":
                                eqType = "LT";
                                break;
                            case ">":
                                eqType = "GT";
                                break;
                            case "!=":
                                eqType = "NE";
                                break;
                            case "==":
                                eqType = "EQ";
                                break;
                            case "<=":
                                eqType = "LE";
                                break;
                            case ">=":
                                eqType = "GE";
                                break;
                        }
                        quad.AddRow("", eqType, leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        if (firstRowComment)
                        {
                            commentRow = quad.GetBotRow(); // sets the first operations in the Statement, this is where our comment should start
                            firstRowComment = false;
                        }
                    }
                    // handle all relational statements
                }
                // Verify previous value is valid for a math operator
                if (inToken.thisType.Equals(TokenType.MATH_OPERATORS))
                {
                    if (stack.Peek().symbol.data[0][1] != "int")
                    {
                        genError(inToken.lineNum, stack.Peek().symbol.data[0][1], "int");
                    }
                }

                oStack.Push(new SARoperator(inToken));
                // TODO handle precedence here
            }
            else
            {
                genError(inToken.lineNum, inToken.lexeme, "valid operator");
            }
        }
        /*
            Push a type onto the Semantic Action Record
         */
        public void TPush(string type)
        {
            SAR_Type type1 = new SAR_Type(type);
            if (null == stack)
            {
                stack = new Stack<SARbase>();
            }
            stack.Push(type1);
        }
        public void EoE()
        {
            // End of expression perform shunting yard algorithm
            SARoperator sOp;
            SARbase rightSide;
            SARbase leftSide;
            if (null == oStack)
            {
                oStack = new Stack<SARbase>();
            }
            //TODO VERIFY CORRECTNESS
            if (oStack.Count == 0)
            {
                while (stack.Count != 0)
                {
                    stack.Pop();
                }
            }
            /*
                Perform Shunting Yard Algorithm
             */
            while (oStack.Count != 0)
            {
                sOp = (SARoperator)oStack.Pop();
                SARtemp tempie;
                if (sOp.token.lexeme == "(" && stack.Count == 0)
                {
                    commentRow[5] = comment;
                    comment = "";
                    return;
                }
                rightSide = stack.Pop();
                leftSide = stack.Pop();
                if (leftSide is SAR_Func && stack.Count > 0 && sOp.token.lexeme == "=")
                {
                    leftSide = stack.Pop();
                }
                Symbol addSym = null;
                switch (sOp.token.lexeme)
                {
                    case "=":
                        if (leftSide.symbol.kind == "this")
                        {
                            if (rightSide.symbol.kind == "null")
                            {
                                genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.lexeme, rightSide.symbol.lexeme);
                            }
                            string[] scopeArr = leftSide.symbol.scope.Split(".");
                            string scopie = "";
                            string type = scopeArr[scopeArr.Length - 2];
                            for (int i = 0; i < scopeArr.Length - 1; i++)
                            {
                                scopie += scopeArr[i];
                                if (i < scopeArr.Length - 2)
                                {
                                    scopie += ".";
                                }
                            }
                            SARinstance addie = null;
                            foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
                            {
                                if (s.Value.lexeme == type && s.Value.scope == scopie)
                                {
                                    addie = new SARinstance(scopie + "." + type, (Symbol)s.Value.Clone());
                                    addie.symbol.data[0] = new List<string>();
                                    addie.symbol.data[0].Add("type:");
                                    addie.symbol.data[0].Add(type);
                                }
                            }
                            oStack.Push(sOp);
                            if (addie != null)
                            {
                                stack.Push(addie);
                            }
                            else
                            {
                                genInvalidOp(sOp.token.lineNum, scopeArr[scopeArr.Length - 1], "this", sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                            }
                            stack.Push(rightSide);
                        }
                        else if (rightSide.symbol.kind == "null" && (leftSide.symbol.data[0][1] == "int" || leftSide.symbol.data[0][1] == "bool" || leftSide.symbol.data[0][1] == "char" || leftSide.symbol.data[0][1] == "void" || leftSide.symbol.data[0][1] == "sym"))
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.kind == "null" && rightSide.symbol.kind != "this")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.kind == "this" && rightSide.symbol.kind != "null")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.kind == "null" && rightSide.symbol.kind == "this")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.lexeme, rightSide.symbol.lexeme);
                        }
                        else if (rightSide.symbol.scope == "g.main" && rightSide.symbol.lexeme == "this")
                        {
                            genSAMErrorIEXIST(sOp.token.lineNum, "Variable", "this");
                        }
                        else if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                        {
                            if (leftSide.xId == "return")
                            {
                                if (rightSide.symbol.data[0][1] == "@:")
                                {
                                    genSAMErrorRET(sOp.token.lineNum, leftSide.symbol.data[0][1], rightSide.symbol.data[0][2] + "[]");
                                }
                                genSAMErrorRET(sOp.token.lineNum, leftSide.symbol.data[0][1], rightSide.symbol.data[0][1]);
                            }
                            else if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                            {
                                genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                            }
                            else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                            {
                                genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                            }
                            else
                            {
                                genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                            }
                        }
                        else if (leftSide.symbol.kind == "clit")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.kind == "ilit")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.kind == "true")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.kind == "false")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.kind == "null")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.kind == "this")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                        {
                            // handle arrays
                            if (leftSide.symbol.data[0][1].Equals("@:") && rightSide.symbol.data[0][1].Equals("@:"))
                            {
                                if (!leftSide.symbol.data[0][2].Equals(rightSide.symbol.data[0][2]))
                                {
                                    genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                                }
                            }
                        }
                        if (!firstRowComment)
                        {
                            commentRow[5] = comment;
                            comment = "";
                            firstRowComment = true;
                        }
                        // if we have no assignment errors add a MOV to the quad table (handle return here)
                        if (leftSide.xId != "return")
                        {
                            quad.AddRow("", "MOV", leftSide.symbol.symid, rightSide.symbol.symid, "", comment);
                            comment = "";
                        }
                        else
                        {
                            quad.AddRow("", "RETURN", rightSide.symbol.symid, "", "", comment);
                            comment = "";
                        }
                        break;
                    case "+":
                        if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                        }
                        if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]))
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                        // We add a new temp variable here the activation record needs SPACE for this
                        addSym = (Symbol)tempie.symbol.Clone();
                        addSym.symid = tempie.xId;
                        addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                        addSym.byteSize = 4;
                        addSym.curOffset = tempVarOffset;
                        symbolHashSet.Add(addSym.symid, addSym);
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        tempVarCounter++;
                        if (!firstRowComment)
                        {
                            commentRow[5] = comment;
                            comment = "";
                            firstRowComment = true;
                        }
                        quad.AddRow("", "ADD", leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        comment = "";
                        stack.Push(tempie);

                        break;
                    case "-":
                        if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                        }
                        if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                        // We add a new temp variable here the activation record needs SPACE for this
                        addSym = (Symbol)tempie.symbol.Clone();
                        addSym.symid = tempie.xId;
                        addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                        addSym.byteSize = 4;
                        addSym.curOffset = tempVarOffset;
                        tempie.symbol = addSym;
                        symbolHashSet.Add(addSym.symid, addSym);
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        tempVarCounter++;
                        if (!firstRowComment)
                        {
                            commentRow[5] = comment;
                            comment = "";
                            firstRowComment = true;
                        }
                        quad.AddRow("", "SUB", leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        comment = "";
                        stack.Push(tempie);
                        break;
                    case "*":
                        if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                        }
                        if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                        // We add a new temp variable here the activation record needs SPACE for this
                        addSym = (Symbol)tempie.symbol.Clone();
                        addSym.symid = tempie.xId;
                        addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                        addSym.byteSize = 4;
                        addSym.curOffset = tempVarOffset;
                        tempie.symbol = addSym;
                        symbolHashSet.Add(addSym.symid, addSym);
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        tempVarCounter++;
                        if (!firstRowComment)
                        {
                            commentRow[5] = comment;
                            comment = "";
                            firstRowComment = true;
                        }
                        quad.AddRow("", "MUL", leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        comment = "";
                        stack.Push(tempie);
                        break;
                    case "/":
                        if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                        }
                        if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                        // We add a new temp variable here the activation record needs SPACE for this
                        addSym = (Symbol)tempie.symbol.Clone();
                        addSym.symid = tempie.xId;
                        addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                        addSym.byteSize = 4;
                        addSym.curOffset = tempVarOffset;
                        tempie.symbol = addSym;
                        symbolHashSet.Add(addSym.symid, addSym);
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        tempVarCounter++;
                        if (!firstRowComment)
                        {
                            commentRow[5] = comment;
                            comment = "";
                            firstRowComment = true;
                        }
                        quad.AddRow("", "DIV", leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        comment = "";
                        stack.Push(tempie);
                        break;
                    case "<":
                    case ">":
                    case "<=":
                    case ">=":
                    case "!=":
                    case "==":
                        if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                        }
                        if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                        {

                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                        // We add a new temp variable here the activation record needs SPACE for this
                        addSym = (Symbol)tempie.symbol.Clone();
                        addSym.symid = tempie.xId;
                        addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                        addSym.byteSize = 4;
                        addSym.curOffset = tempVarOffset;
                        tempie.symbol = addSym;
                        symbolHashSet.Add(addSym.symid, addSym);
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        tempVarCounter++;

                        stack.Push(tempie);
                        // verify comment
                        string eqType = "";
                        switch (sOp.token.lexeme)
                        {
                            case "<":
                                eqType = "LT";
                                break;
                            case ">":
                                eqType = "GT";
                                break;
                            case "!=":
                                eqType = "NE";
                                break;
                            case "==":
                                eqType = "EQ";
                                break;
                            case "<=":
                                eqType = "LE";
                                break;
                            case ">=":
                                eqType = "GE";
                                break;
                        }

                        if (!firstRowComment)
                        {
                            commentRow[5] = comment;
                            comment = "";
                            firstRowComment = true;
                        }
                        quad.AddRow("", eqType, leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        comment = "";
                        break;
                    case "and":
                    case "or":
                        if (rightSide.symbol.data[0][1].Equals("null"))
                        {
                            genSAMErrorANDOR(sOp.token.lineNum, sOp.token.lexeme, leftSide.symbol.data[0][1]);
                        }
                        else if (!leftSide.symbol.data[0][1].Equals("bool"))
                        {
                            genSAMErrorANDOR(sOp.token.lineNum, sOp.token.lexeme, leftSide.symbol.data[0][1]);
                        }
                        else if (!rightSide.symbol.data[0][1].Equals("bool"))
                        {
                            genSAMErrorANDOR(sOp.token.lineNum, sOp.token.lexeme, leftSide.symbol.data[0][1]);
                        }
                        tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                        // We add a new temp variable here the activation record needs SPACE for this
                        addSym = (Symbol)tempie.symbol.Clone();
                        addSym.symid = tempie.xId;
                        addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                        addSym.byteSize = 4;
                        addSym.curOffset = tempVarOffset;
                        tempie.symbol = addSym;
                        symbolHashSet.Add(addSym.symid, addSym);
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        tempVarCounter++;
                        if (!firstRowComment)
                        {
                            commentRow[5] = comment;
                            comment = "";
                            firstRowComment = true;
                        }
                        quad.AddRow("", sOp.token.lexeme.ToUpper(), leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        stack.Push(tempie);
                        break;
                    default:
                        genError(sOp.token.lineNum, sOp.token.lexeme, "valid operator");
                        break;
                }
            }
            if (quad.GetBotRow()[1] == "PEEK")
            {
                quad.RemoveBotRow();
            }
        }
        /*
            condense instances of args to single arg
         */
        public void CommaAction()
        {
            while (oStack.Peek().token.lexeme != "(")
            {
                SYA();
            }
        }
        /*
         * EAL for function call
         */
        public void EAL(int lineNum)
        {
            SARbase rightSide = null;
            SARbase leftSide = null;
            SAR_AL rightArgs = null;
            SAR_Type leftType = null;
            /*
             handle isntance where last arg has math operators
             */
            while (oStack.Peek().token.lexeme != "(")
            {
                SYA();
            }
            if (rowStored != null)
            {
                rowStored[5] = comment;
                comment = "";
                quad.AddRow(rowStored[0].ToString(), rowStored[1].ToString(), rowStored[2].ToString(), rowStored[3].ToString(), rowStored[4].ToString(), rowStored[5].ToString());
            }
            /*
                Place args in al_SAR (argument list SAR) NOTE: May need to fix quad
             */
            SAR_AL argList = new SAR_AL("al_sar");
            Stack<string> revParamPush = new Stack<string>();
            while (!(stack.Peek() is SAR_BAL))
            {
                SARbase tempSAR = stack.Pop();
                revParamPush.Push(tempSAR.symbol.symid);
                argList.args.Insert(0, tempSAR);
            }

            if (refFuncError)
            {
                stack.Pop();
                genSAMErrorMemRefFunc(lineNum, "Function", refErrorName, stack.Peek().symbol.data[0][1], argList);
            }

            while (revParamPush.Count != 0)
            {
                string paramId = revParamPush.Pop();
                quad.AddRow("", "PUSH", paramId, "", "", "");
            }
            stack.Pop();
            stack.Push(argList);
            /*
                Combine Arg list with func arg or if we are doing a new combine into a newSAR
             */
            if (!newFlag)
            {
                argList = (SAR_AL)stack.Pop();
                leftSide = stack.Pop();
                SAR_Func func = new SAR_Func("func_sar", leftSide.symbol, argList);
                stack.Push(func);
                /*
                 * Verify Paramters
                 */
                int indexie = 1;
                for (int i = 0; i < func.funcArgs.args.Count; i++)
                {
                    try
                    {
                        Symbol s = symbolHashSet[func.symbol.data[1][indexie]];
                        if (!s.data[0][1].Equals(func.funcArgs.args[i].symbol.data[0][1]))
                        {
                            genSAMErrorIEXISTFunc(lineNum, leftSide.symbol.lexeme, argList.args, leftSide.symbol.scope.Remove(0, 2));
                        }
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        genSAMErrorIEXISTFunc(lineNum, leftSide.symbol.lexeme, argList.args, leftSide.symbol.scope.Remove(0, 2));
                    }
                    catch (KeyNotFoundException e)
                    {
                        genSAMErrorIEXISTFuncMemRef(oStack.Peek().token.lineNum, refErrorName, argList.args);
                    }
                    indexie = indexie + 2; // note that in the symbol for a method the paramaters start at index 1, and each even index is a comma 
                }
                if (func.funcArgs.args.Count != ((leftSide.symbol.data[1].Count - 1) / 2))
                {
                    genSAMErrorIEXISTFunc(lineNum, leftSide.symbol.lexeme, argList.args, leftSide.symbol.scope.Remove(0, 2));
                }
                rightSide = stack.Pop();
                if (stack.Count == 0 && oStack.Peek().token.lexeme == "(")
                {
                    quad.AddRow("", "CALL", rightSide.symbol.symid, "", "", "");
                    return;
                }
                leftSide = stack.Pop();
                // add these to our hashset if we are popping Sarrefs

                if (leftSide is SARref)
                {
                    Symbol addie = new Symbol(leftSide.symbol.scope, "T" + tempVarCounter, rightSide.symbol.lexeme, leftSide.symbol.kind);
                    for (int i = 0; i < 3; i++)
                    {
                        foreach (string s in leftSide.symbol.data[i])
                        {
                            addie.data[i].Add(s);
                        }
                    }
                    addie.byteSize = 4;
                    addie.curOffset = tempVarOffset;
                    if (insideMethod)
                    {
                        tempVarOffset -= 4;
                    }
                    else
                    {
                        tempVarOffset += 4;
                    }
                    symbolHashSet.Add(addie.symid, addie);
                    tempVarCounter++;
                }
                if (rightSide is SARref)
                {
                    Symbol addie = new Symbol(rightSide.symbol.scope, "T" + tempVarCounter, rightSide.symbol.lexeme, rightSide.symbol.kind);
                    for (int i = 0; i < 3; i++)
                    {
                        foreach (string s in rightSide.symbol.data[i])
                        {
                            addie.data[i].Add(s);
                        }
                    }
                    addie.byteSize = 4;
                    addie.curOffset = tempVarOffset;
                    if (insideMethod)
                    {
                        tempVarOffset -= 4;
                    }
                    else
                    {
                        tempVarOffset += 4;
                    }
                    symbolHashSet.Add(addie.symid, addie);
                    tempVarCounter++;
                }
                bool noUpdate = false;
                if (!(leftSide is SARref) && !(rightSide is SARref) && insideConstructor && stack.Count == 0)
                {
                    stack.Push(leftSide);
                    stack.Push(rightSide);
                    noUpdate = true;
                }

                if (incomingMemberRef == false && !(leftSide is SARliteral))
                {
                    stack.Push(leftSide);
                    Symbol instance = new Symbol(scope, "this", "this", "this");
                    stack.Push(new SARliteral("this", instance));
                    leftSide = stack.Pop();
                }

                incomingMemberRef = false;
                stack.Push(new SARref("ref_sar", leftSide, rightSide));
                SARref updateRet = (SARref)stack.Pop();
                // set the return type equal to the method return type
                updateRet.symbol.data[0][1] = updateRet.storedData[1].symbol.data[0][1];
                updateRet.symbol.scope = "g." + rightSide.symbol.data[0][1];

                updateRet.xId = leftSide.symbol.lexeme + "." + rightSide.symbol.lexeme;
                updateRet.symbol.lexeme = leftSide.symbol.lexeme + "." + rightSide.symbol.lexeme;

                // include the params as the data
                updateRet.symbol.data[2].Add("param: [");
                if (argList.args.Count != 0)
                {
                    updateRet.symbol.data[2].Add(argList.args[0].symbol.symid);
                }
                for (int i = 1; i < argList.args.Count; i++)
                {
                    updateRet.symbol.data[2].Add(",");
                    updateRet.symbol.data[2].Add(argList.args[i].symbol.symid); // in the EAL we are adding the argList.args[i] add the symbol to the quad
                }

                updateRet.symbol.data[2].Add("]");
                if (!noUpdate)
                {
                    stack.Push(updateRet);
                }
                oStack.Pop();
            }
            else
            {
                rightArgs = (SAR_AL)stack.Pop();
                leftType = (SAR_Type)stack.Pop();

                // Find the constructor
                Symbol constructor = null;
                bool foundCons = false;
                foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
                {

                    if (s.Value.lexeme.Equals(leftType.xId) && s.Value.symid[0].Equals('X'))
                    {
                        constructor = s.Value;
                        int argIndex = 0;
                        // Verify that the found contruct has a matching arglist
                        if (rightArgs.args.Count == 0 && constructor.data[1].Count == 2)
                        {
                            // if both the constructor and the call to new have no params, handle that case
                            foundCons = true;
                            break;
                        }
                        for (int i = 1; i < constructor.data[1].Count - 1; i = i + 2, argIndex++)
                        {
                            Symbol param = symbolHashSet[constructor.data[1][i]];
                            if (argIndex < rightArgs.args.Count)
                            {
                                if (((constructor.data[1].Count - 1) / 2) != rightArgs.args.Count)
                                {
                                    foundCons = false;
                                    break;
                                }
                                else
                                {
                                    foundCons = true;
                                }
                                constructor = s.Value;
                                if (!rightArgs.args[argIndex].symbol.data[0][1].Equals(param.data[0][1]))
                                {
                                    foundCons = false;
                                    break;
                                }
                            }
                            else
                            {
                                foundCons = false;
                            }
                        }

                        if (foundCons)
                        {
                            break;
                        }
                    }
                }

                /*
                    We found the constructor!
                 */
                if (foundCons)
                {
                    // remove the paren in the oStack
                    oStack.Pop();
                    SAR_NEW addie = new SAR_NEW("new_sar", leftType, rightArgs);
                    addie.symbol = (Symbol)constructor.Clone();
                    //
                    stack.Push(addie);
                }
                else
                {
                    if (leftSide == null)
                    {
                        genSAMErrorIEXISTFuncConst(lineNum, leftType.xId, rightArgs, leftType.xId);
                    }
                    else
                    {
                        genSAMErrorIEXISTFuncConst(lineNum, leftType.xId, rightArgs, leftSide.symbol.scope.Remove(0, 2));
                    }
                }
                newFlag = false;
            }

            if (leftSide is null)
            {
                // TODO handle new CALL
                quad.AddRow("", "CALL", FindConstbyLexeme(leftType.xId), "", "", "");
            }
            else
            {
                quad.AddRow("", "CALL", rightSide.symbol.symid, "", "", "");
            }

            // the rightside should be a temp var (function return)
            rightSide = stack.Pop();
            Symbol addSym = null;
            if (stack.Count != 0)
            {
                leftSide = stack.Peek();
                addSym = (Symbol)rightSide.symbol.Clone();
            }
            else
            {
                addSym = (Symbol)rightSide.symbol.Clone();
            }
            // at this point push a temp
            // We add a new temp variable here the activation record needs SPACE for this
            addSym.lexeme = rightSide.symbol.lexeme;
            addSym.symid = "T" + tempVarCounter;
            addSym.byteSize = 4;
            addSym.curOffset = tempVarOffset;
            if (addSym.data[0].Count == 0)
            {
                for (int i = 0; i < rightSide.symbol.data.Length; i++)
                {
                    foreach (string s in rightSide.symbol.data[i])
                    {
                        addSym.data[i].Add(s);
                    }
                }
            }
            symbolHashSet.Add(addSym.symid, addSym);
            if (insideMethod)
            {
                tempVarOffset -= 4;
            }
            else
            {
                tempVarOffset += 4;
            }
            SARinstance tempie = new SARinstance(addSym.symid, addSym);
            stack.Push(tempie);
            tempVarCounter++;
            if (leftType != null)
            {
                // OBJECT CREATION PEEK
                quad.AddRow("", "PEEK", addSym.symid, "", "", "");
            }
            else
            {
                quad.AddRow("", "PEEK", addSym.symid, "", "", "");
            }
        }

        /*
            BAL SAR push (adds beginnign arg list ot stack)
         */
        public void BALSARpush()
        {
            stack.Push(new SAR_BAL("bal_sar"));
            // This servers as a marker for when our arg starts
        }

        /*
            EAL SAR push (adds beginnign arg list ot stack)
         */
        public void EALSARpush()
        {
            /*
                Self Note: Considering refactoring the following code if time permits, repeated code with the while condition being the main siginificant change
             */
            SARoperator sOp;

            SARbase rightSide;
            SARbase leftSide;
            bool unHitBal = true;
            List<SARbase> args = new List<SARbase>();
            // Customized for EAL, do not replace with SYA
            while (unHitBal)
            {
                sOp = (SARoperator)oStack.Pop();
                SARtemp tempie;
                Symbol addSym = null;
                switch (sOp.token.lexeme)
                {
                    case "=":

                        rightSide = stack.Pop();
                        leftSide = stack.Pop();

                        if (leftSide is SARref)
                        {
                            Symbol addie = new Symbol(leftSide.symbol.scope, "T" + tempVarCounter, leftSide.symbol.lexeme, leftSide.symbol.kind);
                            for (int i = 0; i < 3; i++)
                            {
                                foreach (string s in rightSide.symbol.data[i])
                                {
                                    addie.data[i].Add(s);
                                }
                            }
                            addie.byteSize = 4;
                            addie.curOffset = tempVarOffset;
                            if (insideMethod)
                            {
                                tempVarOffset -= 4;
                            }
                            else
                            {
                                tempVarOffset += 4;
                            }
                            symbolHashSet.Add(addie.symid, addie);
                            tempVarCounter++;
                        }

                        if (rightSide is SARref)
                        {
                            Symbol addie = new Symbol(rightSide.symbol.scope, "T" + tempVarCounter, rightSide.symbol.lexeme, rightSide.symbol.kind);
                            for (int i = 0; i < 3; i++)
                            {
                                foreach (string s in rightSide.symbol.data[i])
                                {
                                    addie.data[i].Add(s);
                                }
                            }
                            addie.byteSize = 4;
                            addie.curOffset = tempVarOffset;
                            if (insideMethod)
                            {
                                tempVarOffset -= 4;
                            }
                            else
                            {
                                tempVarOffset += 4;
                            }
                            symbolHashSet.Add(addie.symid, addie);
                            tempVarCounter++;
                        }

                        if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.kind == "clit")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.kind == "ilit")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.kind == "true")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.kind == "false")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.kind == "null")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.kind == "this")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        break;
                    case "+":
                        rightSide = stack.Pop();
                        leftSide = stack.Pop();
                        if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                        }
                        if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]))
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                        // We add a new temp variable here the activation record needs SPACE for this
                        addSym = (Symbol)tempie.symbol.Clone();
                        addSym.symid = tempie.xId;
                        addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                        addSym.byteSize = 4;
                        addSym.curOffset = tempVarOffset;
                        if (leftSide.symbol.scope.Length < scope.Length)
                        {
                            addSym.scope = scope;
                        }
                        symbolHashSet.Add(addSym.symid, addSym);
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        tempVarCounter++;
                        quad.AddRow("", "ADD", leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        if (firstRowComment)
                        {
                            commentRow = quad.GetBotRow(); // sets the first operations in the Statement, this is where our comment should start
                            firstRowComment = false;
                        }
                        comment = "";
                        stack.Push(tempie);

                        break;
                    case "-":
                        rightSide = stack.Pop();
                        leftSide = stack.Pop();
                        if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                        }
                        if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                        // We add a new temp variable here the activation record needs SPACE for this
                        addSym = (Symbol)tempie.symbol.Clone();
                        addSym.symid = tempie.xId;
                        addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                        addSym.byteSize = 4;
                        addSym.curOffset = tempVarOffset;
                        if (leftSide.symbol.scope.Length < scope.Length)
                        {
                            addSym.scope = scope;
                        }
                        symbolHashSet.Add(addSym.symid, addSym);
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        tempVarCounter++;
                        quad.AddRow("", "SUB", leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        if (firstRowComment)
                        {
                            commentRow = quad.GetBotRow(); // sets the first operations in the Statement, this is where our comment should start
                            firstRowComment = false;
                        }
                        comment = "";
                        stack.Push(tempie);
                        break;
                    case "*":
                        rightSide = stack.Pop();
                        leftSide = stack.Pop();
                        if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                        }
                        if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                        // We add a new temp variable here the activation record needs SPACE for this
                        addSym = (Symbol)tempie.symbol.Clone();
                        addSym.symid = tempie.xId;
                        addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                        addSym.byteSize = 4;
                        addSym.curOffset = tempVarOffset;
                        if (leftSide.symbol.scope.Length < scope.Length)
                        {
                            addSym.scope = scope;
                        }
                        symbolHashSet.Add(addSym.symid, addSym);
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        tempVarCounter++;
                        quad.AddRow("", "MUL", leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        if (firstRowComment)
                        {
                            commentRow = quad.GetBotRow(); // sets the first operations in the Statement, this is where our comment should start
                            firstRowComment = false;
                        }
                        comment = "";
                        stack.Push(tempie);
                        break;
                    case "/":
                        rightSide = stack.Pop();
                        leftSide = stack.Pop();
                        if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                        }
                        if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                        // We add a new temp variable here the activation record needs SPACE for this
                        addSym = (Symbol)tempie.symbol.Clone();
                        addSym.symid = tempie.xId;
                        addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                        addSym.byteSize = 4;
                        addSym.curOffset = tempVarOffset;
                        if (leftSide.symbol.scope.Length < scope.Length)
                        {
                            addSym.scope = scope;
                        }
                        symbolHashSet.Add(addSym.symid, addSym);
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        tempVarCounter++;
                        quad.AddRow("", "DIV", leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        if (firstRowComment)
                        {
                            commentRow = quad.GetBotRow(); // sets the first operations in the Statement, this is where our comment should start
                            firstRowComment = false;
                        }
                        comment = "";
                        stack.Push(tempie);
                        break;
                    case "<":
                    case ">":
                    case "<=":
                    case ">=":
                    case "!=":
                    case "==":
                        rightSide = stack.Pop();
                        leftSide = stack.Pop();
                        if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                        }
                        if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                        tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                        // We add a new temp variable here the activation record needs SPACE for this
                        addSym = (Symbol)tempie.symbol.Clone();
                        addSym.symid = tempie.xId;
                        addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                        addSym.byteSize = 4;
                        addSym.curOffset = tempVarOffset;
                        if (leftSide.symbol.scope.Length < scope.Length)
                        {
                            addSym.scope = scope;
                        }
                        symbolHashSet.Add(addSym.symid, addSym);
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        tempVarCounter++;

                        stack.Push(tempie);
                        string type = "";

                        switch (sOp.token.lexeme)
                        {
                            case "<":
                                type = "LT";
                                break;
                            case ">":
                                type = "GT";
                                break;
                            case "!=":
                                type = "NE";
                                break;
                            case "==":
                                type = "EQ";
                                break;
                            case "<=":
                                type = "LE";
                                break;
                            case ">=":
                                type = "GE";
                                break;
                        }
                        quad.AddRow("", type, leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        if (firstRowComment)
                        {
                            commentRow = quad.GetBotRow(); // sets the first operations in the Statement, this is where our comment should start
                            firstRowComment = false;
                        }
                        comment = "";

                        break;
                    case "and":
                    case "or":
                        rightSide = stack.Pop();
                        leftSide = stack.Pop();
                        if (rightSide.symbol.data[0][1].Equals("null"))
                        {
                            genSAMErrorANDOR(sOp.token.lineNum, sOp.token.lexeme, leftSide.symbol.data[0][1]);
                        }
                        else if (!leftSide.symbol.data[0][1].Equals("bool"))
                        {
                            genSAMErrorANDOR(sOp.token.lineNum, sOp.token.lexeme, leftSide.symbol.data[0][1]);
                        }
                        else if (!rightSide.symbol.data[0][1].Equals("bool"))
                        {
                            genSAMErrorANDOR(sOp.token.lineNum, sOp.token.lexeme, leftSide.symbol.data[0][1]);
                        }
                        tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                        // We add a new temp variable here the activation record needs SPACE for this
                        addSym = (Symbol)tempie.symbol.Clone();
                        addSym.symid = tempie.xId;
                        addSym.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                        addSym.byteSize = 4;
                        addSym.curOffset = tempVarOffset;
                        symbolHashSet.Add(addSym.symid, addSym);
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        tempVarCounter++;

                        stack.Push(tempie);

                        if (!firstRowComment)
                        {
                            commentRow[5] = comment;
                            comment = "";
                            firstRowComment = true;
                        }
                        quad.AddRow("", sOp.token.lexeme.ToUpper(), leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                        break;
                    case "(":
                        rightSide = stack.Pop();
                        if (stack.Count == 0)
                        {
                            unHitBal = false;
                            break;
                        }
                        leftSide = stack.Pop();
                        if (leftSide.xId == "bal_sar")
                        {
                            unHitBal = false;
                            args.Add(rightSide);
                            if (stack.Count == 0)
                            {
                                if (ifNestedExpression == true)
                                {
                                    stack.Push(rightSide);
                                }
                                return;
                            }
                            leftSide = stack.Pop();
                            for (int i = 0; i < (leftSide.symbol.data[1].Count / 2); i++)
                            {
                                if ((leftSide.symbol.data[1].Count / 2) != args.Count)
                                {
                                    string func = leftSide.symbol.lexeme + "(";

                                    for (int j = 0; j < args.Count; j++)
                                    {
                                        if (args[j].symbol.data[0][1] != "@:")
                                        {
                                            func += args[j].symbol.data[0][1];
                                        }
                                        else
                                        {
                                            func += args[j].symbol.data[0][2];
                                            func += "[]";
                                        }
                                        if (j < args.Count - 1)
                                        {
                                            func += ",";
                                        }
                                    }

                                    func += ")";
                                    genSAMErrorIEXIST(sOp.token.lineNum, "Function", func);
                                }
                                else
                                {
                                    // Handle expressions
                                    if (leftSide is SARinstance)
                                    {
                                        stack.Push(leftSide);
                                        stack.Push(rightSide);
                                        foreach (SARbase basie in args)
                                        {
                                            if (methodCall == true)
                                            {
                                                quad.AddRow("", "PUSH", basie.symbol.symid, "", "", "");
                                            }
                                            if (firstRowComment)
                                            {
                                                firstRowComment = false;
                                                commentRow = quad.GetBotRow();
                                            }
                                        }
                                        return;
                                    }
                                    else if (leftSide is SARliteral || leftSide is SARtemp)
                                    {
                                        stack.Push(leftSide);
                                        stack.Push(rightSide);
                                        return;
                                    }
                                    if (methodCall == true && args[i].symbol.data[0][1] != symbolHashSet[leftSide.symbol.data[1][(i * 2 + 1)]].data[0][1])
                                    {
                                        genSAMErrorIEXIST(sOp.token.lineNum, "Function", leftSide.symbol.lexeme + "()");
                                    }
                                }
                            }
                            // Get the containing function
                            // Check args match params 
                            stack.Push(leftSide);
                            break;
                        }
                        else if (rightSide is SAR_BAL)
                        {
                            stack.Push(leftSide);
                            if ((leftSide.symbol.data[1].Count / 2) > 1)
                            {
                                genSAMErrorIEXIST(sOp.token.lineNum, "Function", leftSide.symbol.lexeme + "()");
                            }
                            unHitBal = false;
                            break;
                        }
                        else
                        {
                            oStack.Push(sOp);
                            args.Insert(0, rightSide);
                            stack.Push(leftSide);
                            break;
                        }
                    default:
                        genError(sOp.token.lineNum, sOp.token.lexeme, "valid operator");
                        break;
                }
            }

            if (memFuncError)
            {
                genSAMErrorIEXISTFuncMemRef(oStack.Peek().token.lineNum, refErrorName, args);
            }
        }
        /*
           add a return push that has the containing methods return type 
         */
        public void RetPush(string _scope)
        {
            /*
                Find containing method (will have the scope above) 
             */
            string[] scope = _scope.Split(".");
            bool found = false;
            Symbol method = null;

            foreach (KeyValuePair<string, Symbol> s in symbolHashSet)
            {
                string[] otherScope = s.Value.scope.Split(".");

                for (int i = 0; i < scope.Length - 1; i++)
                {
                    if (i < otherScope.Length)
                    {
                        found = true;
                    }
                    else
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {

                    if (scope[scope.Length - 1].Equals(s.Value.lexeme))
                    {
                        bool notDup = true;
                        for (int i = 0; i < otherScope.Length; i++)
                        {
                            if (scope[i] != otherScope[i])
                            {
                                notDup = false;
                            }
                        }
                        if (notDup)
                        {
                            method = s.Value;
                            break;
                        }
                    }
                    found = false;
                }
            }
            if (null == stack)
            {
                stack = new Stack<SARbase>();
            }
            // at this point we should push a temp variable and call to the quad
            SARinstance retInst = new SARinstance("return", method);
            retInst.symbol = (Symbol)method.Clone();
            stack.Push(retInst);

        }

        public void CloseSwitch()
        {

            while (oStack.Peek().token.lexeme != "(")
            {
                SYA();
            }
            // Semantic Error should be <line_number> + ": switch requires int or char got " <error_type> 
            // TODO OPTIONAL Note: Optional to implement handle after semester
            oStack.Pop();
            stack.Pop();
        }
        /*
            # call to stack
         */
        public void endArr(string scope)
        {
            SARoperator sOp;
            SARtemp tempie;
            SARbase rightSide;
            SARbase leftSide;
            while (oStack.Peek().token.lexeme != "[")
            {
                SYA();
            }
            // Handle initialization (int[] x = ...)

            // Verify result insde "[]" is an int
            if (stack.Peek().symbol.data[0][1] != "int")
            {
                genSAMErrorArr(oStack.Peek().token.lineNum, stack.Peek().symbol.data[0][1]);
            }
            int curLineStore = oStack.Pop().token.lineNum;// pop array begin
            SARbase arrComb;
            // set the type equal to the left value Cat[] x = new Cat[r]; type should now be Cat
            if (newFlag)
            {
                rightSide = stack.Pop();
                leftSide = stack.Pop();
                // AL shoud always be one in the case of arrays (expression decomposed to int)
                SAR_AL arrArgs = new SAR_AL(leftSide.xId);
                arrArgs.args.Add(rightSide);
                arrArgs.symbol = (Symbol)rightSide.symbol.Clone();
                arrComb = new SAR_NEW(leftSide.xId + "[" + rightSide.xId + "]", (SAR_Type)leftSide, arrArgs);
                arrComb.symbol = (Symbol)rightSide.symbol.Clone();
                arrComb.symbol.data[0][1] = leftSide.xId;

                stack.Push(arrComb);
                // CREATING A NEW ARRAY
                tempie = new SARtemp("T" + tempVarCounter, rightSide, new SARoperator(new Token("*", TokenType.MATH_OPERATORS, oStack.Peek().token.lineNum)), rightSide);
                Symbol addie = (Symbol)tempie.symbol.Clone();
                addie.symid = tempie.xId;
                addie.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + getNumLiteral(4).symid;
                addie.byteSize = 4;
                addie.scope = scope;
                addie.curOffset = tempVarOffset;
                if (insideMethod)
                {
                    tempVarOffset -= 4;
                }
                else
                {
                    tempVarOffset += 4;
                }
                symbolHashSet.Add(addie.symid, addie);
                tempVarCounter++;
                SARbase temp1 = stack.Pop();
                if (stack.Peek().symbol.data[0][2] == "char")
                {
                    stack.Push(temp1);
                    quad.AddRow("", "MUL", getNumLiteral(1).symid, rightSide.symbol.symid, addie.symid, comment);
                }
                else
                {
                    stack.Push(temp1);
                    quad.AddRow("", "MUL", getNumLiteral(4).symid, rightSide.symbol.symid, addie.symid, comment);
                }
                comment = "";
                tempie = new SARtemp("T" + tempVarCounter, rightSide, new SARoperator(new Token("*", TokenType.MATH_OPERATORS, oStack.Peek().token.lineNum)), rightSide);
                Symbol malloc = (Symbol)tempie.symbol.Clone();
                malloc.symid = tempie.xId;
                malloc.lexeme = "malloc " + addie.symid;
                malloc.byteSize = 4;
                malloc.scope = scope;
                malloc.curOffset = tempVarOffset;
                if (insideMethod)
                {
                    tempVarOffset -= 4;
                }
                else
                {
                    tempVarOffset += 4;
                }
                symbolHashSet.Add(malloc.symid, malloc);
                tempVarCounter++;
                quad.AddRow("", "NEW", malloc.symid, addie.symid, "", "");

                SARoperator verifyEq = (SARoperator)oStack.Pop();
                if (verifyEq.token.lexeme != "=")
                {
                    genError(verifyEq.token.lineNum, verifyEq.token.lexeme, "=");
                }
                // Verify types
                rightSide = stack.Pop();
                leftSide = stack.Pop();

                quad.AddRow("", "MOV", leftSide.symbol.symid, malloc.symid, "", "");

                if (leftSide.symbol.data[0][2] != rightSide.symbol.data[0][1])
                {
                    genError(verifyEq.token.lineNum, rightSide.symbol.data[0][1], leftSide.symbol.data[0][2]);
                }

            }
            else if (memberArr)
            {
                rightSide = stack.Pop();
                leftSide = stack.Pop();
                if (leftSide is SARref && checkie != 2)
                {
                    Symbol addie = new Symbol(leftSide.symbol.scope, "T" + tempVarCounter, leftSide.xId, leftSide.symbol.kind);
                    for (int i = 0; i < 3; i++)
                    {
                        foreach (string s in leftSide.symbol.data[i])
                        {
                            addie.data[i].Add(s);
                        }
                    }
                    addie.byteSize = 4;
                    addie.curOffset = tempVarOffset;
                    if (insideMethod)
                    {
                        tempVarOffset -= 4;
                    }
                    else
                    {
                        tempVarOffset += 4;
                    }
                    symbolHashSet.Add(addie.symid, addie);
                    tempVarCounter++;
                }
                if (rightSide is SARref)
                {
                    Symbol addie = new Symbol(rightSide.symbol.scope, "T" + tempVarCounter, rightSide.xId, rightSide.symbol.kind);
                    for (int i = 0; i < 3; i++)
                    {
                        foreach (string s in rightSide.symbol.data[i])
                        {
                            addie.data[i].Add(s);
                        }
                    }
                    addie.byteSize = 4;
                    addie.curOffset = tempVarOffset;
                    if (insideMethod)
                    {
                        tempVarOffset -= 4;
                    }
                    else
                    {
                        tempVarOffset += 4;
                    }
                    symbolHashSet.Add(addie.symid, addie);
                    tempVarCounter++;
                }
                // COME BACK HERE
                if (rightSide.symbol.data[0][1] != "int")
                {
                    genError(curLine, rightSide.symbol.data[0][1], "int");
                }

                if (leftSide.symbol.data[0][1] != "@:")
                {
                    genSAMErrorIEXIST(curLineStore, "Array", leftSide.symbol.lexeme); // When we assign on a identifier as an array, but it is not an array
                }
                arrComb = new SARref(leftSide.xId + "[" + rightSide.xId + "]", rightSide, leftSide);
                arrComb.symbol.kind = leftSide.symbol.kind;
                arrComb.symbol.lexeme = leftSide.symbol.lexeme + "[" + rightSide.symbol.lexeme + "]";
                arrComb.symbol.scope = leftSide.symbol.scope;
                arrComb.symbol.symid = leftSide.symbol.symid;
                arrComb.symbol.data[0].Remove("@:");

                stack.Push(arrComb);

                Symbol temp = arrComb.symbol;
                temp.symid = "R" + tempVarCounter;
                temp.byteSize = 4;
                temp.curOffset = tempVarOffset;
                if (insideMethod)
                {
                    tempVarOffset -= 4;
                }
                else
                {
                    tempVarOffset += 4;
                }
                tempVarCounter++;
                symbolHashSet.Add(temp.symid, temp);
                quad.AddRow("", "AEF", leftSide.symbol.symid, rightSide.symbol.symid, temp.symid, "");
                if (firstRowComment)
                {
                    commentRow = quad.GetBotRow();
                    firstRowComment = false;
                }
            }
            else
            {

                rightSide = stack.Pop();
                leftSide = stack.Pop();
                if (leftSide.symbol.data[0][1] != "@:")
                {
                    genSAMErrorIEXIST(curLineStore, "Array", leftSide.symbol.lexeme); // When we call on a identifier as an array, but it is not an array
                }
                arrComb = new SARArr(leftSide.xId + "[" + rightSide.xId + "]", leftSide, rightSide);
                arrComb.symbol.data[0][1] = leftSide.symbol.data[0][2];
                arrComb.symbol.kind = leftSide.symbol.kind;
                arrComb.symbol.lexeme = leftSide.symbol.lexeme + "[" + rightSide.symbol.lexeme + "]";
                arrComb.symbol.scope = leftSide.symbol.scope;
                arrComb.symbol.symid = leftSide.symbol.symid;

                stack.Push(arrComb);

                Symbol temp = arrComb.symbol;
                temp.symid = "R" + tempVarCounter;
                temp.byteSize = 4;
                temp.curOffset = tempVarOffset;
                if (insideMethod)
                {
                    tempVarOffset -= 4;
                }
                else
                {
                    tempVarOffset += 4;
                }
                tempVarCounter++;
                symbolHashSet.Add(temp.symid, temp);
                quad.AddRow("", "AEF", leftSide.symbol.symid, rightSide.symbol.symid, temp.symid, "");
                if (firstRowComment)
                {
                    commentRow = quad.GetBotRow();
                    firstRowComment = false;
                }
            }
            if (newFlag)
            {

                newFlag = false;
                // TODO: if we arent creating new, handle it
            }
        }
        /*
            Handle Cin
         */
        public void CIn(int curLine)
        {

            // End of expression perform shunting yard algorithm
            SARoperator sOp;
            SARbase rightSide;
            SARbase leftSide = null;
            if (null == oStack)
            {
                oStack = new Stack<SARbase>();
            }
            //TODO VERIFY CORRECTNESS
            if (oStack.Count == 0)
            {
                while (stack.Count != 0)
                {
                    if (stack.Peek() is SAR_Type)
                    {
                        stack.Pop();
                        continue;
                    }
                    leftSide = stack.Pop();

                    if (stack.Count == 0)
                    {
                        if (leftSide.symbol.kind == "clit")
                        {
                            genSAMErrorCIN(curLine, "clit");
                        }
                        else if (leftSide.symbol.kind == "ilit")
                        {
                            genSAMErrorCIN(curLine, "ilit");
                        }
                        else if (leftSide.symbol.kind == "true")
                        {
                            genSAMErrorCIN(curLine, "true");
                        }
                        else if (leftSide.symbol.kind == "false")
                        {
                            genSAMErrorCIN(curLine, "false");
                        }
                        else if (leftSide.symbol.kind == "null")
                        {
                            genSAMErrorCIN(curLine, "null");
                        }
                        else if (leftSide.symbol.kind == "this")
                        {
                            genSAMErrorCIN(curLine, "this");
                        }
                    }

                    if (leftSide.symbol.data[0][1] == "char")
                    {
                        quad.AddRow("", "READC", leftSide.symbol.symid, "", "", comment);
                        comment = "";
                    }
                    else if (leftSide.symbol.data[0][1] == "int")
                    {
                        quad.AddRow("", "READI", leftSide.symbol.symid, "", "", comment);
                        comment = "";
                    }
                }
            }
            /*
                Perform Shunting Yard Algorithm
             */
            while (oStack.Count != 0)
            {
                SYA();
            }


            if (!(leftSide.symbol.data[0][1] == "int" || leftSide.symbol.data[0][1] == "char"))
            {
                genSAMErrorCIN(curLine, leftSide.symbol.data[0][1]);
            }
        }

        /*
            Handle Cout - NOTE: in its current state it may be the same as Cin, probably going to change in iCode generation
         */
        public void COut(int curLine)
        {
            // End of expression perform shunting yard algorithm
            if (null == oStack)
            {
                oStack = new Stack<SARbase>();
            }
            //TODO VERIFY CORRECTNESS
            if (oStack.Count == 0)
            {
                while (stack.Count != 0)
                {
                    stack.Pop();
                }
            }
            /*
                Perform Shunting Yard Algorithm
             */
            while (oStack.Count != 0)
            {
                SYA();
            }
        }
        /*
            Verify if has a bool
         */

        public void IfCheck(int _lineNum)
        {
            while (oStack.Peek().token.lexeme != "(")
            {
                SYA();
            }

            SARbase expresisonRes = stack.Pop();
            // add control structure
            quad.AddRow("", "BF", expresisonRes.symbol.symid, "SKIPIF" + quad.labelCounter, "", comment);
            comment = "";
            commentRow = null;
            firstRowComment = true;
            quad.labelStack.Push("SKIPIF" + quad.labelCounter);
            quad.labelCounter++;


            if (expresisonRes.symbol.data[0][1] != "bool")
            {
                genSAMErrorIF(_lineNum, expresisonRes.symbol.data[0][1]);
            }

            string[] scopeVer = scope.Split(".");

            if (scopeVer[scopeVer.Length - 1] == expresisonRes.symbol.lexeme)
            {
                genSAMErrorIF(_lineNum, "null");
            }


            oStack.Pop();
        }
        /*
            Verify while has a bool
         */

        public void WhileCheck(int _lineNum)
        {
            string beginWhile = "BEGINWHILE" + quad.labelCounter;
            quad.labelCounter++;

            while (oStack.Peek().token.lexeme != "(")
            {
                SYA();
            }

            DataRow row = quad.GetBotRow();

            if (row[0].ToString() == "")
            {
                row[0] = beginWhile;
            }
            else
            {
                quad.BackPatch(row[0].ToString(), beginWhile);
            }


            SARbase expresisonRes = stack.Pop();
            // add control structure
            quad.AddRow("", "BF", expresisonRes.symbol.symid, "ENDWHILE" + quad.labelCounter, "", comment);
            if (firstRowComment)
            {
                firstRowComment = false;
                commentRow = quad.GetBotRow();
            }
            comment = "";
            string endWhile = "ENDWHILE" + quad.labelCounter;
            quad.labelCounter++;

            quad.labelStack.Push(endWhile);
            quad.labelStack.Push(beginWhile);

            if (expresisonRes.symbol.data[0][1] != "bool")
            {
                genSAMErrorWHILE(_lineNum, expresisonRes.symbol.data[0][1]);
            }

            oStack.Pop();
        }
        /*
            Same error generation as before
         */
        public void genError(int curLine, string found, string expectation)
        {
            Console.WriteLine(curLine + ": Found " + found + " expecting " + expectation);
            System.Environment.Exit(-1);
        }
        /*
            IEXIST Error
         */
        public void genSAMErrorIEXIST(int curLine, string type, string found)
        {
            Console.WriteLine(curLine + ":  " + type + " " + found + " not defined");
            System.Environment.Exit(-1);
        }
        /*
            IEXIST Func Error for Member Ref
         */
        public void genSAMErrorIEXISTFuncMemRef(int curLine, string lexeme, List<SARbase> args)
        {
            string s = curLine + ": Function " + lexeme + "(";
            for (int i = 0; i < args.Count; i++)
            {
                s += args[i].symbol.data[0][1];
                if (i < args.Count - 1)
                {
                    s += ',';
                }
            }
            s += ")";
            s += " not defined";
            Console.WriteLine(s);
            System.Environment.Exit(-1);
        }
        /*
            IEXIST Func Error
         */
        public void genSAMErrorIEXISTFunc(int curLine, string lexeme, List<SARbase> args, string scope)
        {
            string s = curLine + ": Function " + lexeme + "(";
            for (int i = 0; i < args.Count; i++)
            {
                s += args[i].symbol.data[0][1];
                if (i < args.Count - 1)
                {
                    s += ',';
                }
            }
            s += ")";
            s += " not defined in class ";
            s += scope;
            Console.WriteLine(s);
            System.Environment.Exit(-1);
        }
        /*
            IEXIST Func Error for class
         */
        public void genSAMErrorIEXISTFuncConst(int curLine, string lexeme, SAR_AL args, string scope)
        {
            string s = curLine + ": Constructor " + lexeme + "(";
            for (int i = 0; i < args.args.Count; i++)
            {
                s += args.args[i].symbol.data[0][1];
                if (i < args.args.Count - 1)
                {
                    s += ',';
                }
            }
            s += ")";
            s += " not defined";
            Console.WriteLine(s);
            System.Environment.Exit(-1);
        }
        /*
            DUPLICATE error generation as before
         */
        public void genSAMErrorDUP(int curLine, string type, string found)
        {
            Console.WriteLine(curLine + ":  Duplicate " + type + " " + found);
            System.Environment.Exit(-1);
        }
        /*
            WHILE error generation as before
         */
        public void genSAMErrorWHILE(int curLine, string type)
        {
            Console.WriteLine(curLine + ":  while requires bool got " + type);
            System.Environment.Exit(-1);
        }
        /*
            IF error generation as before
         */
        public void genSAMErrorIF(int curLine, string type)
        {
            Console.WriteLine(curLine + ":  if requires bool got " + type);
            System.Environment.Exit(-1);
        }
        /*
            Return error generation as before
         */
        public void genSAMErrorRET(int curLine, string type, string errorType)
        {
            if (!classes.Contains(errorType) && errorType != "bool" && errorType != "int" && errorType != "char" && errorType != "int" && errorType != "void" && errorType != "sym")
            {
                Console.WriteLine(curLine + ": Type " + errorType + " not defined");
                System.Environment.Exit(-1);
            }
            Console.WriteLine(curLine + ":  Function requires " + type + " returned " + errorType);
            System.Environment.Exit(-1);
        }
        /*
            cOut error generation as before
         */
        public void genSAMErrorCOUT(int curLine, string type)
        {
            Console.WriteLine(curLine + ":  cout not defined for " + type);
            System.Environment.Exit(-1);
        }
        /*
            cIn error generation as before
         */
        public void genSAMErrorCIN(int curLine, string type)
        {
            Console.WriteLine(curLine + ":  cin not defined for " + type);
            System.Environment.Exit(-1);
        }
        /*
            Array Index Error
         */
        public void genSAMErrorArr(int curLine, string type)
        {
            Console.WriteLine(curLine + ":  Array requires int index got " + type);
            System.Environment.Exit(-1);
        }

        /*
            invalid op
         */
        public void genInvalidOp(int curLine, string type1, string lex1, string opIn, string type2, string lex2)
        {
            string typeCheck = type1;
            if (typeCheck.Contains("[]"))
            {
                typeCheck = typeCheck.Replace("[", "");
                typeCheck = typeCheck.Replace("]", "");
            }

            string typeCheck2 = type2;
            if (typeCheck2.Contains("[]"))
            {
                typeCheck2 = typeCheck2.Replace("[", "");
                typeCheck2 = typeCheck2.Replace("]", "");
            }
            // check TExists
            if (!classes.Contains(type1) && !(typeCheck == "int" || typeCheck == "char" || typeCheck == "bool" || typeCheck == "sym" || typeCheck == "void"))
            {
                if (lex1 == "this") 
                {
                    string[] scopeArr = scope.Split('.');
                    Console.WriteLine(curLine + ":  Invalid Operation " + scopeArr[1] + " " + lex1 + " " + opIn + " " + type2 + " " + lex2);
                    System.Environment.Exit(-1);
                }
                Console.WriteLine(curLine + ": Type " + type1 + " not defined");
                System.Environment.Exit(-1);
            }
            else if (!classes.Contains(typeCheck2) && !(typeCheck2 == "int" || typeCheck2 == "char" || typeCheck2 == "bool" || typeCheck2 == "sym" || typeCheck2 == "void" || typeCheck2 == "null"))
            {
                Console.WriteLine(curLine + ": Type " + type2 + " not defined");
                System.Environment.Exit(-1);
            }
            Console.WriteLine(curLine + ":  Invalid Operation " + type1 + " " + lex1 + " " + opIn + " " + type2 + " " + lex2);
            System.Environment.Exit(-1);
        }
        /*
            and OR
         */
        public void genSAMErrorANDOR(int curLine, string andOr, string type)
        {
            Console.WriteLine(curLine + ": " + andOr + " requires bool found " + type);
            System.Environment.Exit(-1);
        }
        /*
            member Ref Error
         */
        public void genSAMErrorMemRef(int curLine, string type, string lexeme, string containingClass)
        {
            Console.WriteLine(curLine + ": " + type + " " + lexeme + " not defined/public in class " + containingClass);
            System.Environment.Exit(-1);
        }
        /*
            member Func Ref Error
         */
        public void genSAMErrorMemRefFunc(int curLine, string type, string lexeme, string containingClass, SAR_AL args)
        {
            string errorMsg = "" + curLine + ": " + type + " " + lexeme + "(";
            for (int i = 0; i < args.args.Count; i++)
            {
                errorMsg += args.args[i].symbol.data[0][1];
                if (i < args.args.Count - 1)
                {
                    errorMsg += ", ";
                }
            }
            errorMsg += ") not defined/public in class " + containingClass;

            Console.WriteLine(errorMsg);
            System.Environment.Exit(-1);
        }
        /*
            Get the byte size of a class
         */
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
            Get the byte size of a class
         */
        public Symbol getNumLiteral(int num)
        {
            string numStr = "" + num;
            foreach (KeyValuePair<String, Symbol> s in symbolHashSet)
            {
                if (s.Value.scope == "g" && s.Value.lexeme == numStr)
                {
                    return s.Value;
                }
            }
            return null;
        }
        /*
         * 
         */
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
        /*
            Shunting Yard Algorithm, general use, 2 instances where a more specialized version was left. 
         */
        public void SYA()
        {

            SARoperator sOp;
            SARbase rightSide;
            SARbase leftSide;
            SARtemp tempie;

            sOp = (SARoperator)oStack.Pop();
            Symbol addie = null;
            switch (sOp.token.lexeme)
            {
                case "=":
                    rightSide = stack.Pop();
                    leftSide = stack.Pop();

                    if (leftSide is SARref)
                    {
                        addie = new Symbol(leftSide.symbol.scope, "T" + tempVarCounter, leftSide.symbol.lexeme, leftSide.symbol.kind);
                        for (int i = 0; i < 3; i++)
                        {
                            foreach (string s in rightSide.symbol.data[i])
                            {
                                addie.data[i].Add(s);
                            }
                        }
                        addie.byteSize = 4;
                        addie.curOffset = tempVarOffset;
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        symbolHashSet.Add(addie.symid, addie);
                        tempVarCounter++;
                    }

                    if (rightSide is SARref)
                    {
                        addie = new Symbol(rightSide.symbol.scope, "T" + tempVarCounter, rightSide.symbol.lexeme, rightSide.symbol.kind);
                        for (int i = 0; i < 3; i++)
                        {
                            foreach (string s in rightSide.symbol.data[i])
                            {
                                addie.data[i].Add(s);
                            }
                        }
                        addie.byteSize = 4;
                        addie.curOffset = tempVarOffset;
                        if (insideMethod)
                        {
                            tempVarOffset -= 4;
                        }
                        else
                        {
                            tempVarOffset += 4;
                        }
                        symbolHashSet.Add(addie.symid, addie);
                        tempVarCounter++;
                    }

                    if (rightSide.symbol.kind == "this" && rightSide.symbol.data[0].Count == 0)
                    {
                        if (leftSide.symbol.lexeme == "cout")
                        {
                            genSAMErrorCOUT(sOp.token.lineNum, rightSide.symbol.scope);
                        }
                    }
                    else if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                    {
                        if (leftSide.xId == "cout")
                        {
                            if (rightSide.symbol.kind != "true" && rightSide.symbol.kind != "false" && !(rightSide.symbol.data[0][1] == "int" || rightSide.symbol.data[0][1] == "char"))
                            {
                                genSAMErrorCOUT(sOp.token.lineNum, rightSide.symbol.data[0][1]);
                            }
                            else if (!(rightSide.symbol.data[0][1] == "int" || rightSide.symbol.data[0][1] == "char"))
                            {
                                genSAMErrorCOUT(sOp.token.lineNum, rightSide.symbol.kind);
                            }
                            // Cout quad here
                            if (rightSide.symbol.data[0][1] == "char")
                            {
                                quad.AddRow("", "WRITEC", rightSide.symbol.symid, "", "", comment);
                                comment = "";
                            }
                            else if (rightSide.symbol.data[0][1] == "int")
                            {
                                quad.AddRow("", "WRITEI", rightSide.symbol.symid, "", "", comment);
                                comment = "";
                            }

                        }
                        else
                        {
                            genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                        }
                    }
                    else if (leftSide.symbol.kind == "clit")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    else if (leftSide.symbol.kind == "ilit")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    else if (leftSide.symbol.kind == "true")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    else if (leftSide.symbol.kind == "false")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    else if (leftSide.symbol.kind == "null")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    else if (leftSide.symbol.kind == "this")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    else if (leftSide.symbol.lexeme == "cout")
                    {
                        if (rightSide.symbol.kind == "true")
                        {
                            genSAMErrorCOUT(curLine, "true");
                        }
                        else if (rightSide.symbol.kind == "false")
                        {
                            genSAMErrorCOUT(curLine, "false");
                        }
                        else if (rightSide.symbol.kind == "null")
                        {
                            genSAMErrorCOUT(curLine, "null");
                        }
                        else if (rightSide.symbol.kind == "this")
                        {
                            genSAMErrorCOUT(curLine, "this");
                        }
                    }
                    break;
                case "+":
                    rightSide = stack.Pop();
                    leftSide = stack.Pop();
                    if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                    }
                    if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]))
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                    addie = (Symbol)tempie.symbol.Clone();
                    addie.symid = tempie.xId;
                    addie.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                    addie.byteSize = 4;
                    addie.curOffset = tempVarOffset;
                    if (insideMethod)
                    {
                        tempVarOffset -= 4;
                    }
                    else
                    {
                        tempVarOffset += 4;
                    }
                    if (leftSide.symbol.scope.Length < scope.Length)
                    {
                        addie.scope = scope;
                    }
                    symbolHashSet.Add(addie.symid, addie);
                    tempVarCounter++;
                    quad.AddRow("", "ADD", leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                    if (firstRowComment)
                    {
                        firstRowComment = false;
                        commentRow = quad.GetBotRow();
                    }
                    comment = "";
                    stack.Push(tempie);
                    break;
                case "-":
                    rightSide = stack.Pop();
                    leftSide = stack.Pop();
                    if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                    }
                    if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                    addie = (Symbol)tempie.symbol.Clone();
                    addie.symid = tempie.xId;
                    addie.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                    addie.byteSize = 4;
                    addie.curOffset = tempVarOffset;
                    if (insideMethod)
                    {
                        tempVarOffset -= 4;
                    }
                    else
                    {
                        tempVarOffset += 4;
                    }
                    if (leftSide.symbol.scope.Length < scope.Length)
                    {
                        addie.scope = scope;
                    }
                    symbolHashSet.Add(addie.symid, addie);
                    tempVarCounter++;
                    quad.AddRow("", "SUB", leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                    if (firstRowComment)
                    {
                        firstRowComment = false;
                        commentRow = quad.GetBotRow();
                    }
                    comment = "";
                    stack.Push(tempie);
                    break;
                case "*":
                    rightSide = stack.Pop();
                    leftSide = stack.Pop();
                    if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                    }
                    if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                    addie = (Symbol)tempie.symbol.Clone();
                    addie.symid = tempie.xId;
                    addie.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                    addie.byteSize = 4;
                    addie.curOffset = tempVarOffset;
                    if (insideMethod)
                    {
                        tempVarOffset -= 4;
                    }
                    else
                    {
                        tempVarOffset += 4;
                    }
                    if (leftSide.symbol.scope.Length < scope.Length)
                    {
                        addie.scope = scope;
                    }
                    symbolHashSet.Add(addie.symid, addie);
                    tempVarCounter++;
                    quad.AddRow("", "MUL", leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                    if (firstRowComment)
                    {
                        firstRowComment = false;
                        commentRow = quad.GetBotRow();
                    }
                    comment = "";
                    stack.Push(tempie);
                    break;
                case "/":
                    rightSide = stack.Pop();
                    leftSide = stack.Pop();
                    if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                    }
                    if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                    addie = (Symbol)tempie.symbol.Clone();
                    addie.symid = tempie.xId;
                    addie.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                    addie.byteSize = 4;
                    addie.curOffset = tempVarOffset;
                    if (insideMethod)
                    {
                        tempVarOffset -= 4;
                    }
                    else
                    {
                        tempVarOffset += 4;
                    }
                    if (leftSide.symbol.scope.Length < scope.Length)
                    {
                        addie.scope = scope;
                    }
                    symbolHashSet.Add(addie.symid, addie);
                    tempVarCounter++;
                    quad.AddRow("", "DIV", leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                    if (firstRowComment)
                    {
                        firstRowComment = false;
                        commentRow = quad.GetBotRow();
                    }
                    comment = "";
                    stack.Push(tempie);
                    break;
                case "<":
                case ">":
                case "<=":
                case ">=":
                case "!=":
                case "==":
                    rightSide = stack.Pop();
                    leftSide = stack.Pop();
                    if (leftSide.symbol.data[0][1] == "@:" && rightSide.symbol.data[0][1] != "@:")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][2] + "[]", leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    else if (leftSide.symbol.data[0][1] != "@:" && rightSide.symbol.data[0][1] == "@:")
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][2] + "[]", rightSide.symbol.lexeme);
                    }
                    if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                    addie = (Symbol)tempie.symbol.Clone();
                    addie.symid = tempie.xId;
                    addie.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                    addie.byteSize = 4;
                    addie.curOffset = tempVarOffset;
                    if (insideMethod)
                    {
                        tempVarOffset -= 4;
                    }
                    else
                    {
                        tempVarOffset += 4;
                    }
                    symbolHashSet.Add(addie.symid, addie);
                    tempVarCounter++;
                    stack.Push(tempie);
                    string type = "";

                    switch (sOp.token.lexeme)
                    {
                        case "<":
                            type = "LT";
                            break;
                        case ">":
                            type = "GT";
                            break;
                        case "!=":
                            type = "NE";
                            break;
                        case "==":
                            type = "EQ";
                            break;
                        case "<=":
                            type = "LE";
                            break;
                        case ">=":
                            type = "GE";
                            break;
                    }
                    quad.AddRow("", type, leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                    comment = "";
                    if (firstRowComment)
                    {
                        firstRowComment = false;
                        commentRow = quad.GetBotRow();
                    }

                    break;
                case "and":
                case "or":
                    rightSide = stack.Pop();
                    leftSide = stack.Pop();
                    if (rightSide.symbol.data[0][1].Equals("null"))
                    {
                        genSAMErrorANDOR(sOp.token.lineNum, sOp.token.lexeme, leftSide.symbol.data[0][1]);
                    }
                    else if (!leftSide.symbol.data[0][1].Equals("bool"))
                    {
                        genSAMErrorANDOR(sOp.token.lineNum, sOp.token.lexeme, leftSide.symbol.data[0][1]);
                    }
                    else if (!rightSide.symbol.data[0][1].Equals("bool"))
                    {
                        genSAMErrorANDOR(sOp.token.lineNum, sOp.token.lexeme, leftSide.symbol.data[0][1]);
                    }
                    tempie = new SARtemp("T" + tempVarCounter, leftSide, sOp, rightSide);
                    addie = (Symbol)tempie.symbol.Clone();
                    addie.symid = tempie.xId;
                    addie.lexeme = tempie.storedData[0].xId + tempie.storedData[1].token.lexeme + tempie.storedData[2].xId;
                    addie.byteSize = 4;
                    addie.curOffset = tempVarOffset;
                    if (insideMethod)
                    {
                        tempVarOffset -= 4;
                    }
                    else
                    {
                        tempVarOffset += 4;
                    }
                    symbolHashSet.Add(addie.symid, addie);
                    tempVarCounter++;
                    stack.Push(tempie);
                    quad.AddRow("", sOp.token.lexeme.ToUpper(), leftSide.symbol.symid, rightSide.symbol.symid, tempie.xId, comment);
                    comment = "";
                    if (firstRowComment)
                    {
                        firstRowComment = false;
                        commentRow = quad.GetBotRow();
                    }
                    break;
                case "(":
                    rightSide = stack.Pop();
                    stack.Pop();
                    leftSide = stack.Pop();
                    if (leftSide.xId == "bal_sar")
                    {
                        List<SARbase> args = new List<SARbase>();
                        args.Add(rightSide);
                        while (leftSide.symbol.symid[0] != 'M' && stack.Count > 0)
                        {

                        }
                        // Get the containing function
                        // Check args match params 
                    }
                    else if (!leftSide.symbol.data[0][1].Equals(rightSide.symbol.data[0][1]) && !rightSide.symbol.data[0][1].Equals("null"))
                    {
                        genInvalidOp(sOp.token.lineNum, leftSide.symbol.data[0][1], leftSide.symbol.lexeme, sOp.token.lexeme, rightSide.symbol.data[0][1], rightSide.symbol.lexeme);
                    }
                    stack.Push(leftSide);
                    stack.Push(rightSide);
                    break;
                default:
                    genError(sOp.token.lineNum, sOp.token.lexeme, "valid operator");
                    break;
            }
        }
    }

}