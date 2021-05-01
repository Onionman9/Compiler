using System;
using System.Collections.Generic;
using System.IO;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            String inFile = "testme.kxi";

            if (args.Length > 0)
            {
                inFile = args[0];
            }
            else
            {
                Console.WriteLine("No arguments passed, what file would you like to compile.");
                inFile = Console.ReadLine();
                if (!File.Exists(inFile))
                {
                    Console.WriteLine("No file found - Closing Compiler");

                    return;
                }
                else 
                {
                
                }
            }

            Lexicator Lexie = new Lexicator();
            
            // Verify the file type
            
            string[] verifyType = inFile.Split('.');

            if (verifyType[verifyType.Length - 1] != "kxi") 
            {

                Console.WriteLine("File: " + inFile + " is not a .ksi file and can not be compiled... closing!");
                return;
            }

            Lexie.Execute(inFile);
            Console.Clear();
            SymbolTable loadTable = new SymbolTable();
            loadTable.Execute();


        }
    }
    ///   <summary>
    ///    The Lexicator Class performs the lexical analysis of the input program
    ///    </summary>
    class Lexicator 
    {
        // Debug toggle
        private bool DEBUG = false; // Setting to true will result in step by step checking.

        public List<string> keywords = new List<string>();
        public List<string> modifier = new List<string>();
        public List<string> types = new List<string>();
        public List<string> class_names = new List<string>();
        public List<string> identifiers = new List<string>();
        public List<string> statements = new List<string>();
        public List<string> character_literals = new List<string>();
        public List<string> numberic_literal = new List<string>();
        public List<char> numbers = new List<char>();
        public List<char> printable_ascii = new List<char>();
        public List<char> identifierChars = new List<char>();
        public List<char> unprintable_ascii = new List<char>();

        public Token curToken;
        public Token peekToken;
        public Token peek2Token;
        /*
            Lexicator Constructor - This will populare our symbols before we begin parsing a file
         */

        public Lexicator()
        {
            ItializeLists();
        }
        ///   <summary>
        ///    The itializeLists function runs all our Populator functions
        ///    </summary>
        private void ItializeLists() 
        {
            KeywordPop();
            ModifierPop();
            TypesPop();
            NumbersPop();
            PrintASCIIpop();
            UnprintASCIIpop();
            identifierPop();
        }
        ///   <summary>
        ///    The KeywordPop function populates the keyword list with all our language keywords
        ///    </summary>
        private void KeywordPop() 
        {
            if (keywords.Count != 0) 
            {
                return;
            }

            // Populate keywords on Lexicator initialization

            keywords.Add("atoi");
            keywords.Add("and");
            keywords.Add("bool");
            keywords.Add("block");
            keywords.Add("break");

            keywords.Add("case");
            keywords.Add("class");
            keywords.Add("char");
            keywords.Add("cin");
            keywords.Add("cout");

            keywords.Add("default");
            keywords.Add("else");
            keywords.Add("false");
            keywords.Add("if");
            keywords.Add("int");

            keywords.Add("default");
            keywords.Add("else");
            keywords.Add("false");
            keywords.Add("if");
            keywords.Add("int");

            keywords.Add("itoa");
            keywords.Add("kxi2021");
            keywords.Add("lock");
            keywords.Add("main");
            keywords.Add("new");

            keywords.Add("null");
            keywords.Add("object");
            keywords.Add("or");
            keywords.Add("public");
            keywords.Add("private");

            keywords.Add("protected");
            keywords.Add("return");
            keywords.Add("release");
            keywords.Add("string");
            keywords.Add("spawn");

            keywords.Add("sym");
            keywords.Add("set");
            keywords.Add("switch");
            keywords.Add("this");
            keywords.Add("true");

            keywords.Add("thread");
            keywords.Add("unprotected");
            keywords.Add("unlock");
            keywords.Add("void");
            keywords.Add("while");

            keywords.Add("wait");
            keywords.Add("EOF");
        }
        ///   <summary>
        ///    The ModifierPop function populates the modifier list with all our language keywords
        ///    </summary>
        private void ModifierPop() 
        {
            if (modifier.Count != 0)
            {
                return;
            }

            // populate modifiers

            modifier.Add("public");
            modifier.Add("private");
        }
        ///   <summary>
        ///    The TypesPop function populates the types list with all our language modifiers
        ///    </summary>
        private void TypesPop()
        {
            if (types.Count != 0)
            {
                return;
            }

            // populate types

            types.Add("int");
            types.Add("char");
            types.Add("bool");
            types.Add("void");
            types.Add("sym");
        }
        ///   <summary>
        ///    The NumbersPop function populates the numbers list with all standard digits
        ///    </summary>
        private void NumbersPop()
        {
            if (numbers.Count != 0)
            {
                return;
            }

            // populate numbers

            numbers.Add('0');
            numbers.Add('1');
            numbers.Add('2');
            numbers.Add('3');
            numbers.Add('4');
            numbers.Add('5');
            numbers.Add('6');
            numbers.Add('7');
            numbers.Add('8');
            numbers.Add('9');
        }
        ///   <summary>
        ///    The printASCIIpop function populates the printable ascii list with all PRINTABLE ASCII chars
        ///    </summary>
        private void PrintASCIIpop()
        {
            if (printable_ascii.Count != 0)
            {
                return;
            }

            // populate printable ASCII values

            int asciInter = 32;
            while (asciInter < 127)
            {
                char c = Convert.ToChar(asciInter);
                if (DEBUG && false) // Anded with false so to reduce debug clutter
                {
                    Console.WriteLine(" SYMBOL: " + c);
                }
                printable_ascii.Add(c);
                asciInter++;
            }

        }
        ///   <summary>
        ///    The unprintASCIIpop function populates the unprintable ascii list with all UNPRINTABLE ASCII chars
        ///    </summary>
        private void UnprintASCIIpop()
        {
            if (unprintable_ascii.Count != 0)
            {
                return;
            }

            // populate unprintable ASCII values

            int asciInter = 127;
            unprintable_ascii.Add(Convert.ToChar(asciInter));
            asciInter = 0;
            while (asciInter < 32)
            {
                char c = Convert.ToChar(asciInter);
                if (DEBUG && false) // Anded with false so to reduce debug clutter
                {
                    Console.WriteLine(" SYMBOL: " + c);
                }
                unprintable_ascii.Add(c);
                asciInter++;
            }

        }
        ///   <summary>
        ///    The identifierPop function populates the identifer acceptable ascii list with all acceptable chars
        ///    </summary
        private void identifierPop()
        {
            if (identifierChars.Count != 0)
            {
                return;
            }

            // populate unprintable ASCII values

            int asciInter = 48;
            while (asciInter < 58)
            {
                char c = Convert.ToChar(asciInter);
                if (DEBUG && false) // Anded with false so to reduce debug clutter
                {
                    Console.WriteLine(" SYMBOL: " + c);
                }
                identifierChars.Add(c);
                asciInter++;
            }

            asciInter = 65;
            while (asciInter < 91)
            {
                char c = Convert.ToChar(asciInter);
                if (DEBUG && false) // Anded with false so to reduce debug clutter
                {
                    Console.WriteLine(" SYMBOL: " + c);
                }
                identifierChars.Add(c);
                asciInter++;
            }
            asciInter = 97;
            while (asciInter < 123)
            {
                char c = Convert.ToChar(asciInter);
                if (DEBUG && false) // Anded with false so to reduce debug clutter
                {
                    Console.WriteLine(" SYMBOL: " + c);
                }
                identifierChars.Add(c);
                asciInter++;
            }
        }
        ///   <summary>
        ///    The Lexicator Class Execute method performs the Lexical Analysis
        ///    </summary>
        public void Execute(string inFile) 
        {
            StreamReader reader = null;
            try
            {
                reader = new StreamReader(inFile);
            }
            catch (FileNotFoundException) 
            {
                Console.WriteLine("File: " + inFile + " not found!");
                System.Environment.Exit(-1);
            }
            StreamWriter sw = new StreamWriter("ET1.txt");
            /*
             Begin Lexical Analysis - Runs until we hit the end of the file
             */
            int lineCounter = 0;
            string curWord = "";
            bool numberFlag = true;
            bool commentFlag = false;
            bool aposFlag = false;
            bool EOFflag = false;


            curToken = null;

            while (!reader.EndOfStream) 
            {
                // If we reached the enfo a file (Or it has been flagged early set the token to EOF)

                // Seperate by whitespace
                lineCounter++;
                string curLine = reader.ReadLine();
                // process by character
                int currentIndex = 0;

                // LEXICAL ANALYSIS
                if (EOFflag)
                {
                    peek2Token = new Token(curWord, TokenType.EOF, lineCounter);
                    if (DEBUG)
                    {
                        Console.WriteLine(curToken.ToString());
                    }
                    if (curToken != null)
                    {
                        sw.WriteLine(curToken.ToString());// makes a log for testing
                        sw.Flush();
                    }
                    // Console.WriteLine(curToken.ToString());
                    curToken = peekToken;
                    peekToken = peek2Token;
                    peek2Token = null;
                    curWord = "";
                    peek2Token = null;
                    numberFlag = true;
                }
                else
                {
                    for (int i = currentIndex; i < curLine.Length; i++)
                    {
                        currentIndex = i;
                        char curChar = curLine[i];
                        /*
                            Check if we have a number on a new segment of text
                         */
                        if (!numbers.Contains(curChar) && identifierChars.Contains(curChar) && curWord.Length == 0)
                        {
                            numberFlag = false;
                        }

                        if (curWord == "" && curChar == ' ' || curWord == "" && curChar == '\t') // if we have whitespace and an empty currentWord, just skip
                        {
                            continue;
                        }
                        else if (!numbers.Contains(curChar) && identifierChars.Contains(curChar) && numberFlag)
                        {
                            peek2Token = new Token(curWord, TokenType.NUMBER, lineCounter);

                            i--;
                            currentIndex--;
                        }
                        else if (curWord != "" && !(identifierChars.Contains(curChar))) // If we have content and hit a seperating character, BEGIN ANALYSIS
                        {
                            if (keywords.Contains(curWord))
                            {
                                // Keyword Type Check
                                if (modifier.Contains(curWord))
                                {
                                    if (curWord == "class")
                                    {
                                        peek2Token = new Token(curWord, TokenType.CLASS_DECLARATION, lineCounter);
                                    }
                                    else
                                    {
                                        peek2Token = new Token(curWord, TokenType.KEYWORDS, lineCounter);
                                    }

                                }
                                else if (types.Contains(curWord))
                                {
                                    peek2Token = new Token(curWord, TokenType.KEYWORDS, lineCounter);
                                }
                                else if (class_names.Contains(curWord))
                                {
                                    peek2Token = new Token(curWord, TokenType.IDENTIFIER, lineCounter);
                                }
                                else if (class_names.Contains(curWord))
                                {
                                    peek2Token = new Token(curWord, TokenType.CLASS_DECLARATION, lineCounter);
                                }
                                else if (curWord == "EOF")
                                {
                                    EOFflag = true;
                                    peek2Token = new Token(curWord, TokenType.EOF, lineCounter);
                                    if (DEBUG)
                                    {
                                        // Console.WriteLine(curToken.ToString());
                                    }
                                }
                                else
                                {
                                    peek2Token = new Token(curWord, TokenType.KEYWORDS, lineCounter);
                                }
                            }
                            else if (curChar == '\'' && aposFlag)
                            {
                                curWord += curChar;
                                i++;
                                currentIndex++;
                                peek2Token = new Token(curWord, TokenType.CHARACTER, lineCounter);
                                aposFlag = false;
                            }
                            else
                            {
                                if (numberFlag)
                                {
                                    peek2Token = new Token(curWord, TokenType.NUMBER, lineCounter);
                                }
                                else
                                {
                                    peek2Token = new Token(curWord, TokenType.IDENTIFIER, lineCounter);
                                }
                            }
                            numberFlag = true;
                            i--;
                            currentIndex--;
                        }
                        else // ELSE we handle single characters that have significance
                        {
                            curWord += curChar;

                            if (curWord.Length == 1)
                            {
                                switch (curChar)
                                {
                                    case '.':
                                    case ',':
                                    case ';':
                                        peek2Token = new Token(curWord, TokenType.PUNCTUATION, lineCounter);
                                        break;
                                    case '{':
                                        peek2Token = new Token(curWord, TokenType.BLOCK_BEGIN, lineCounter);
                                        break;
                                    case '}':
                                        peek2Token = new Token(curWord, TokenType.BLOCK_END, lineCounter);
                                        break;
                                    case '(':
                                        peek2Token = new Token(curWord, TokenType.PARENTHESES_OPEN, lineCounter);
                                        break;
                                    case ')':
                                        peek2Token = new Token(curWord, TokenType.PARENTHESES_CLOSE, lineCounter);
                                        break;
                                    case '[':
                                        peek2Token = new Token(curWord, TokenType.ARRAY_BEGIN, lineCounter);
                                        break;
                                    case ']':
                                        peek2Token = new Token(curWord, TokenType.ARRAY_END, lineCounter);
                                        break;
                                    case '-':
                                    case '*':
                                    case '+':
                                        peek2Token = new Token(curWord, TokenType.MATH_OPERATORS, lineCounter);
                                        break;
                                    case '<':
                                        if ((i + 1) < curLine.Length)
                                        {
                                            if (curLine[i + 1] == '=')
                                            {
                                                curWord += '=';
                                                i++;
                                                peek2Token = new Token(curWord, TokenType.RELATIONAL_OPERATORS, lineCounter);
                                            }
                                            else
                                            {
                                                peek2Token = new Token(curWord, TokenType.RELATIONAL_OPERATORS, lineCounter);
                                            }
                                        }
                                        else
                                        {
                                            peek2Token = new Token(curWord, TokenType.RELATIONAL_OPERATORS, lineCounter);
                                        }
                                        break;
                                    case '>':
                                        if ((i + 1) < curLine.Length)
                                        {
                                            if (curLine[i + 1] == '=')
                                            {
                                                curWord += '=';
                                                i++;
                                                peek2Token = new Token(curWord, TokenType.RELATIONAL_OPERATORS, lineCounter);
                                            }
                                            else
                                            {
                                                peek2Token = new Token(curWord, TokenType.RELATIONAL_OPERATORS, lineCounter);
                                            }
                                        }
                                        else
                                        {
                                            peek2Token = new Token(curWord, TokenType.RELATIONAL_OPERATORS, lineCounter);
                                        }
                                        break;
                                    case '=':
                                        if ((i + 1) < curLine.Length)
                                        {
                                            if (curLine[i + 1] == '=')
                                            {
                                                curWord += '=';
                                                i++;
                                                peek2Token = new Token(curWord, TokenType.RELATIONAL_OPERATORS, lineCounter);
                                            }
                                            else
                                            {
                                                peek2Token = new Token(curWord, TokenType.ASSIGNMENT_OPERATOR, lineCounter);
                                            }
                                        }
                                        else
                                        {
                                            peek2Token = new Token(curWord, TokenType.RELATIONAL_OPERATORS, lineCounter);
                                        }
                                        break;
                                    case '\'':
                                        aposFlag = true;
                                        if (i + 3 < curLine.Length)
                                        {
                                            if (curLine[i + 3] == '\'' && curLine[i + 1] == '\\')
                                            {
                                                aposFlag = false;
                                                curWord += curLine[i + 1];
                                                curWord += curLine[i + 2];
                                                curWord += curLine[i + 3];
                                                peek2Token = new Token(curWord, TokenType.CHARACTER, lineCounter);
                                                i = i + 3;
                                                break;
                                            }
                                        }
                                        if (i + 2 < curLine.Length && aposFlag)
                                        {
                                            if (curLine[i + 2] == '\'')
                                            {
                                                aposFlag = false;
                                                curWord += curLine[i + 1];
                                                curWord += curLine[i + 2];
                                                peek2Token = new Token(curWord, TokenType.CHARACTER, lineCounter);
                                                i = i + 2;
                                                break;
                                            }
                                        }
                                        if (aposFlag)
                                        {
                                            peek2Token = new Token(curWord, TokenType.UNKNOWN, lineCounter);
                                        }
                                        break;
                                    case '!':
                                        if ((i + 1) < curLine.Length)
                                        {
                                            if (curLine[i + 1] == '=')
                                            {
                                                curWord += '=';
                                                i++;
                                                peek2Token = new Token(curWord, TokenType.RELATIONAL_OPERATORS, lineCounter);
                                            }
                                            else
                                            {
                                                peek2Token = new Token(curWord, TokenType.UNKNOWN, lineCounter);
                                            }
                                        }
                                        else
                                        {
                                            peek2Token = new Token(curWord, TokenType.UNKNOWN, lineCounter);
                                        }
                                        break;
                                    case '/':
                                        if ((i + 1) < curLine.Length)
                                        {
                                            if (curLine[i + 1] == '/')
                                            {
                                                curWord = "";
                                                i++;
                                                commentFlag = true;
                                            }
                                            else
                                            {
                                                peek2Token = new Token(curWord, TokenType.MATH_OPERATORS, lineCounter);
                                            }
                                        }
                                        else
                                        {
                                            peek2Token = new Token(curWord, TokenType.MATH_OPERATORS, lineCounter);
                                        }
                                        break;
                                    default:
                                        if (!identifierChars.Contains(curChar))
                                        {
                                            peek2Token = new Token(curWord, TokenType.UNKNOWN, lineCounter);
                                        }
                                        break;
                                }
                            }
                        }
                        if (EOFflag) 
                        {
                            break;
                        }
                        if (commentFlag) 
                        {
                            commentFlag = false;
                            break;
                        }
                        if (curToken != null && peek2Token!= null && DEBUG)
                        {
                            bool responseLoop = true;
                            while (responseLoop)
                            {
                                Console.WriteLine("DEBUG: Press 1 for Current Token, 2 for first peek, 3 for second peek, and 4 to continue");
                                char c = Console.ReadKey().KeyChar;
                                Console.Clear();
                                switch (c)
                                {
                                    case '1':
                                        Console.WriteLine(curToken.ToString());
                                        break;
                                    case '2':
                                        Console.WriteLine(peekToken.ToString());
                                        break;
                                    case '3':
                                        Console.WriteLine(peek2Token.ToString());
                                        break;
                                    case '4':
                                        responseLoop = false;
                                        numberFlag = true;
                                        break;
                                }
                            }
                        }
                        // Move token's up
                        if (peek2Token != null)
                        {
                            if (curToken != null) 
                            {
                                // Console.WriteLine(curToken.ToString());
                                sw.WriteLine(curToken.ToString());// makes a log for testing
                                sw.Flush();
                            }
                            curToken = peekToken;
                            peekToken = peek2Token;
                            peek2Token = null;
                            curWord = "";
                            peek2Token = null;
                            numberFlag = true;
                        }

                    }
                }

            }

            // FINAL TOKEN

            if (peek2Token == null && curWord != "")
            {
                if (keywords.Contains(curWord))
                {
                    // Keyword Type Check
                    if (modifier.Contains(curWord))
                    {
                        if (curWord == "class")
                        {
                            peek2Token = new Token(curWord, TokenType.CLASS_DECLARATION, lineCounter);
                        }
                        else
                        {
                            peek2Token = new Token(curWord, TokenType.KEYWORDS, lineCounter);
                        }

                    }
                    else if (types.Contains(curWord))
                    {
                        peek2Token = new Token(curWord, TokenType.KEYWORDS, lineCounter);
                    }
                    else if (class_names.Contains(curWord))
                    {
                        peek2Token = new Token(curWord, TokenType.IDENTIFIER, lineCounter);
                    }
                    else
                    {
                        peek2Token = new Token(curWord, TokenType.KEYWORDS, lineCounter);
                    }
                }
                else
                {
                    if (numberFlag)
                    {
                        peek2Token = new Token(curWord, TokenType.NUMBER, lineCounter);
                    }
                    else
                    {
                        peek2Token = new Token(curWord, TokenType.IDENTIFIER, lineCounter);
                    }
                }
            }
            else 
            {
                peek2Token = new Token(curWord, TokenType.EOF, lineCounter);
            }

            if (DEBUG)
            {
                while ((!curToken.thisType.Equals(TokenType.EOF)))
                {
                    // Console.WriteLine("DEBUG: Press 1 for Current Token, 2 for first peek, 3 for second peek, and 4 to continue");
                    char c = Console.ReadKey().KeyChar;
                    Console.Clear();
                    switch (c)
                    {
                        case '1':
                            Console.WriteLine(curToken.ToString());
                            break;
                        case '2':
                            Console.WriteLine(peekToken.ToString());
                            break;
                        case '3':
                            Console.WriteLine(peek2Token.ToString());
                            break;
                        case '4':
                            sw.WriteLine(curToken.ToString());// makes a log for testing
                            sw.Flush();
                            curToken = peekToken;
                            peekToken = peek2Token;
                            peek2Token = new Token("", TokenType.EOF, lineCounter);
                            break;
                    }
                }

                Console.WriteLine("");
                if (curWord != "")
                {
                   //  Console.WriteLine(curToken.ToString());
                }
                curToken = new Token("", TokenType.EOF, lineCounter);
                sw.WriteLine(curToken.ToString());// makes a log for testing
                sw.Flush();
                // Console.WriteLine(curToken.ToString());
            }
            else 
            {
                sw.WriteLine(curToken.ToString());// makes a log for testing
                sw.Flush();
                // Console.WriteLine(curToken.ToString());
                while (!curToken.thisType.Equals(TokenType.EOF)) 
                {
                    curToken = peekToken;
                    peekToken = peek2Token;
                    peek2Token = new Token("", TokenType.EOF, lineCounter);
                    // Console.WriteLine(curToken.ToString());
                    sw.WriteLine(curToken.ToString());// makes a log for testing
                    sw.Flush();
                }
            }

            sw.Close();

        }        
    }
}
