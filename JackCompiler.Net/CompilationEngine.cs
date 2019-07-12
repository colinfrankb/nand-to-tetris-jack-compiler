using JackCompiler.Net.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace JackCompiler.Net
{
    public class CompilationEngine
    {
        private readonly SymbolTable _symbolTable;
        private string _className;
        private VMWriter _vmWriter;
        private string _currentExecutingSubroutine;
        private IList<string> _userDefinedSubroutines;

        public CompilationEngine()
        {
            _symbolTable = new SymbolTable();
            _vmWriter = new VMWriter();
            _userDefinedSubroutines = new List<string>();
        }

        public IList<string> Compile(IEnumerable<Token> tokens)
        {
            var tokenStack = new Stack<Token>(tokens.Reverse());

            var instructions = new List<string>();

            var construct = CompileClass(tokenStack);

            instructions.AddRange(construct.ConstructInstructions);

            return instructions;
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileNextToken(Stack<Token> tokens)
        {
            var nextToken = tokens.Peek();

            if (nextToken.TokenType == "keyword")
            {
                if (nextToken.Value == "class")
                {
                    return CompileClass(tokens);
                }

                if (Regex.IsMatch(nextToken.Value, "(field|static)"))
                {
                    return CompileClassVarDec(tokens);
                }

                if (Regex.IsMatch(nextToken.Value, "(constructor|function|method)"))
                {
                    return CompileSubroutine(tokens);
                }

                if (nextToken.Value == "return")
                {
                    return CompileReturn(tokens);
                }

                if (nextToken.Value == "var")
                {
                    return CompileVarDec(tokens);
                }

                if (nextToken.Value == "let")
                {
                    return CompileLet(tokens);
                }

                if (nextToken.Value == "do")
                {
                    return CompileDo(tokens);
                }

                if (nextToken.Value == "while")
                {
                    return CompileWhile(tokens);
                }

                if (nextToken.Value == "if")
                {
                    return CompileIf(tokens);
                }
            }

            throw new NotImplementedException($"This token type is not implemented: {nextToken.TokenType}");
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileIf(Stack<Token> tokens)
        {
            //vm code for ~(expression)
            //if-goto else_begin_for_if (even if there is no else, still go there, the extra step does not make a difference)
            //vm code for executing body of if statement
            //goto if_end
            //label else_begin_for_if
            //vm code for executing body of else statement
            //label if_end
            var instructions = new List<string>();
            var lastTokenValue = string.Empty;
            var ifStatementRunningIndex = _symbolTable.GetNextIfStatementRunningIndex();

            while (tokens.Peek().Value != "{")
            {
                if (lastTokenValue == "(")
                {
                    var expressionTokens = PopExpressionTokensBetweenBrackets("(", ")", tokens);
                    var expression = CompileExpression(expressionTokens);
                    var expressionTree = ExpressionTree.ConvertToXmlDocument(expression).FirstChild;

                    instructions.AddRange(WriteExpression(expressionTree));

                    //same rational as the while loop
                    instructions.Add("not");

                    instructions.AddRange(_vmWriter.WriteIf($"ELSE_BEGIN{ifStatementRunningIndex}"));
                }

                var token = tokens.Pop();

                lastTokenValue = token.Value;
            }

            var bodyTokens = PopStatementBody(tokens); //first token is "{", last token is "}"

            AddStatements(instructions, bodyTokens);

            instructions.AddRange(_vmWriter.WriteGoto($"IF_END{ifStatementRunningIndex}"));
            instructions.AddRange(_vmWriter.WriteLabel($"ELSE_BEGIN{ifStatementRunningIndex}"));

            var nextToken = tokens.Peek();

            if (nextToken.TokenType == "keyword" && nextToken.Value == "else")
            {
                var elseToken = tokens.Pop();
                var elseStatementBodyTokens = PopStatementBody(tokens);

                AddStatements(instructions, elseStatementBodyTokens);
            }

            instructions.AddRange(_vmWriter.WriteLabel($"IF_END{ifStatementRunningIndex}"));

            return ("ifStatement", instructions);
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileWhile(Stack<Token> tokens)
        {
            //label l1
            //vm code for ~(expression)
            //if-goto l2
            //vm code for executing body of while
            //goto l1
            //label l2

            var instructions = new List<string>();
            var whileLoopRunningIndex = _symbolTable.GetNextWhileLoopRunningIndex();

            instructions.AddRange(_vmWriter.WriteLabel($"WHILE_BEGIN{whileLoopRunningIndex}"));

            var lastTokenValue = string.Empty;

            while (tokens.Peek().Value != "{")
            {
                if (lastTokenValue == "(")
                {
                    var expressionTokens = PopExpressionTokensBetweenBrackets("(", ")", tokens);
                    var expression = CompileExpression(expressionTokens);
                    var expressionTree = ExpressionTree.ConvertToXmlDocument(expression).FirstChild;

                    instructions.AddRange(WriteExpression(expressionTree));

                    //the true evaluation has to be on the bitwise negation of the result of the expression
                    //because instructions in this VM and assembly code execute from top to bottom
                    //i.e if not goto end of while loop
                    instructions.Add("not");

                    instructions.AddRange(_vmWriter.WriteIf($"WHILE_END{whileLoopRunningIndex}"));
                }

                var token = tokens.Pop();
                
                lastTokenValue = token.Value;
            }

            var bodyTokens = PopStatementBody(tokens);

            AddStatements(instructions, bodyTokens);

            instructions.AddRange(_vmWriter.WriteGoto($"WHILE_BEGIN{whileLoopRunningIndex}"));
            instructions.AddRange(_vmWriter.WriteLabel($"WHILE_END{whileLoopRunningIndex}"));

            return ("whileStatement", instructions);
        }

        private void AddStatements(List<string> instructions, Stack<Token> bodyTokens)
        {
            bodyTokens.Pop(); // pop opening bracket

            while (bodyTokens.Count > 1)
            {
                var (constructType, constructInstructions) = CompileNextToken(bodyTokens);

                instructions.AddRange(constructInstructions);
            }

            bodyTokens.Pop(); // pop closing bracket
        }

        private Stack<Token> PopStatementBody(Stack<Token> tokens)
        {
            IList<Token> bodyTokens = new List<Token>();

            var indexOfClosingBracket = FindIndexOfClosingBracket(0, tokens);
            var numberOfRemainingTokensAfterSubroutine = tokens.Count - (indexOfClosingBracket + 1);

            //continue to pop tokens that are defined within this while construct
            while (tokens.Count > numberOfRemainingTokensAfterSubroutine)
            {
                var token = tokens.Pop();

                bodyTokens.Add(token);
            }

            return new Stack<Token>(bodyTokens.Reverse());
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileDo(Stack<Token> tokens)
        {
            var instructions = new List<string>();
            var lastTokenValue = string.Empty;
            var subroutineName = string.Empty;
            var expressionList = new List<string>();

            while (lastTokenValue != ";")
            {
                if (lastTokenValue == "(")
                {
                    var expressionListTokens = PopExpressionTokensBetweenBrackets("(", ")", tokens);

                    expressionList.AddRange(CompileExpressionList(expressionListTokens));
                }

                var token = tokens.Pop();

                if (token.TokenType == TokenType.Identifier || token.Value == ".")
                {
                    subroutineName += token.Value;
                }
                
                lastTokenValue = token.Value;
            }

            var expressionTreeList = ExpressionTree.ConvertToXmlDocument(expressionList).FirstChild; // The root is a XmlDocument

            foreach (XmlNode expressionTree in expressionTreeList.ChildNodes)
            {
                instructions.AddRange(WriteExpression(expressionTree));
            }

            instructions.AddRange(_vmWriter.WriteCall(subroutineName, expressionTreeList.ChildNodes.Count));

            if (IsUserDefinedSubroutine(subroutineName))
            {
                _currentExecutingSubroutine = subroutineName;
            }

            return ("doStatement", instructions);
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileLet(Stack<Token> tokens)
        {
            var instructions = new List<string>();
            var identifierName = string.Empty;
            var currentIndex = -1;
            var lastTokenValue = string.Empty;
            var valueExpression = new List<string>();

            while (lastTokenValue != ";")
            {
                currentIndex++;

                if (lastTokenValue == "[")
                {
                    var expressionTokens = PopExpressionTokensBetweenBrackets("[", "]", tokens);

                    instructions.AddRange(CompileExpression(expressionTokens));
                }
                else if (lastTokenValue == "=")
                {
                    var expressionTokens = PopUntil(";", tokens);

                    valueExpression.AddRange(CompileExpression(expressionTokens));
                }

                var token = tokens.Pop();

                if (currentIndex == 1)
                {
                    identifierName = token.Value;
                }

                lastTokenValue = token.Value;
            }

            var valueExpressionTree = ExpressionTree.ConvertToXmlDocument(valueExpression).FirstChild;

            instructions.AddRange(WriteExpression(valueExpressionTree));

            var symbol = _symbolTable.GetSymbolByName(identifierName);

            //TODO: Set an int
            //TODO: Set a string
            //TODO: Set an object
            //TODO: Set a field of an object

            instructions.AddRange(_vmWriter.WritePop(symbol.ToSegment(), symbol.RunningIndex));

            return ("letStatement", instructions);
        }

        private Stack<Token> PopUntil(string targetTokenValue, Stack<Token> tokens)
        {
            IList<Token> expressionTokens = new List<Token>();

            while (tokens.TryPeek(out Token peekedToken) && peekedToken.Value != targetTokenValue)
            {
                var token = tokens.Pop();

                expressionTokens.Add(token);
            }

            return new Stack<Token>(expressionTokens.Reverse());
        }

        public Stack<Token> PopExpressionTokensBetweenBrackets(string openingBracket, string closingBracket, Stack<Token> tokens)
        {
            int currentOpenBrackets = 0;
            IList<Token> expressionTokens = new List<Token>();

            while (tokens.Peek().Value != closingBracket || currentOpenBrackets > 0)
            {
                var token = tokens.Pop();

                if (token.Value == openingBracket)
                {
                    currentOpenBrackets++;
                }
                else if (token.Value == closingBracket)
                {
                    currentOpenBrackets--;
                }

                expressionTokens.Add(token);
            }

            return new Stack<Token>(expressionTokens.Reverse());
        }

        private IList<string> CompileExpressionList(Stack<Token> expressionList)
        {
            var instructions = new List<string>();

            instructions.Add("<expressionList>");

            var expressions = new List<IList<Token>>();
            var currentExpression = new List<Token>();

            while (expressionList.TryPeek(out Token peekedToken))
            {
                //TODO: Determine if the comma is part of a inner subroutine
                if (peekedToken.Value == ",")
                {
                    //pop comma, but it is not used
                    expressionList.Pop();

                    expressions.Add(currentExpression);
                    currentExpression = new List<Token>();
                }
                else
                {
                    currentExpression.Add(expressionList.Pop());
                }
            }

            if (currentExpression.Any())
            {
                expressions.Add(currentExpression);
            }

            for (int i = 0; i < expressions.Count ;i++)
            {
                var expression = expressions[i];

                var expressionAsStack = new Stack<Token>(expression.Reverse());
                instructions.AddRange(CompileExpression(expressionAsStack));
            }

            instructions.Add("</expressionList>");

            return instructions;
        }

        private IList<string> CompileExpression(Stack<Token> expressionTokens)
        {
            var instructions = new List<string>();

            instructions.Add("<expression>");

            while (expressionTokens.TryPop(out Token token))
            {
                if (Regex.IsMatch(token.TokenType, $"({TokenType.IntegerConstant}|{TokenType.StringConstant}|{TokenType.Keyword})"))
                {
                    instructions.Add("<term>");
                    instructions.Add(ToXmlElement(token));
                    instructions.Add("</term>");
                }
                else if (token.TokenType == TokenType.Symbol)
                {
                    if (token.Value == "(")
                    {
                        var termExpression = PopExpressionTokensBetweenBrackets("(", ")", expressionTokens);

                        instructions.Add("<term>");

                        instructions.AddRange(CompileExpression(termExpression));

                        expressionTokens.Pop(); // pop closing bracket

                        instructions.Add("</term>");
                    }
                    else if (token.Value == "-")
                    {
                        var isOperator = instructions.Last() == "</term>";

                        if (!isOperator)
                        {
                            instructions.Add("<term>"); //opening term for expression
                        }

                        instructions.Add(ToXmlElement(token));
                        //following tokens can be a term
                        //will assume integerConstant for now
                        var integerConstantTerm = expressionTokens.Pop();

                        instructions.Add("<term>");
                        instructions.Add(ToXmlElement(integerConstantTerm));
                        instructions.Add("</term>");

                        if (!isOperator)
                        {
                            instructions.Add("</term>"); //closing term for expression
                        }
                    }
                    else if (token.Value == "~")
                    {
                        instructions.Add("<term>"); //opening term for expression

                        instructions.Add(ToXmlElement(token));
                        //following tokens can be a term
                        //will assume integerConstant for now
                        //var integerConstantTerm = expressionTokens.Pop();

                        instructions.Add("<term>");

                        if (expressionTokens.TryPeek(out Token peekedToken) && peekedToken.Value == "(")
                        {
                            expressionTokens.Pop(); // pop opening bracket

                            var innerExpressionTokens = PopExpressionTokensBetweenBrackets("(", ")", expressionTokens);

                            instructions.AddRange(CompileExpression(innerExpressionTokens));

                            expressionTokens.Pop(); // pop closing bracket
                        }
                        else
                        {
                            var integerConstantTerm = expressionTokens.Pop();
                            instructions.Add(ToXmlElement(integerConstantTerm));
                        }
                        
                        instructions.Add("</term>");

                        instructions.Add("</term>"); //closing term for expression
                    }
                    else //therefore the symbol is an operator
                    {
                        instructions.Add(ToXmlElement(token));
                    }
                }
                else if (token.TokenType == TokenType.Identifier) //could be varName | varName[] | subroutineCall
                {
                    if (expressionTokens.TryPeek(out Token peekedToken) && Regex.IsMatch(peekedToken.Value, @"(\[|\.|\()"))
                    {
                        if (peekedToken.Value == "(")
                        {
                            instructions.Add("<term>");
                            instructions.Add(ToXmlElement(token));
                            expressionTokens.Pop(); // pop opening bracket

                            var expressionList = PopExpressionTokensBetweenBrackets("(", ")", expressionTokens);

                            instructions.AddRange(CompileExpressionList(expressionList));

                            expressionTokens.Pop(); // pop closing bracket
                            instructions.Add("</term>");
                        }
                        else if (peekedToken.Value == "[")
                        {
                            instructions.Add("<term>");
                            instructions.Add(ToXmlElement(token));
                            instructions.Add(ToXmlElement(expressionTokens.Pop())); 

                            var arrayExpression = PopExpressionTokensBetweenBrackets("[", "]", expressionTokens);
                            instructions.AddRange(CompileExpression(arrayExpression));

                            instructions.Add(ToXmlElement(expressionTokens.Pop()));
                            instructions.Add("</term>");
                        }
                        else // therefore ".", for example [varName|className].subroutineName()
                        {
                            instructions.Add("<term kind=\"subroutineCall\">");
                            var subroutine = token.Value;
                            subroutine += expressionTokens.Pop().Value; // pop "."
                            subroutine += expressionTokens.Pop().Value; // pop subroutineName
                            instructions.Add(ToXmlElement("subroutine", subroutine));
                            expressionTokens.Pop(); // pop opening bracket

                            var expressionList = PopExpressionTokensBetweenBrackets("(", ")", expressionTokens);
                            instructions.AddRange(CompileExpressionList(expressionList));

                            expressionTokens.Pop(); // pop closing bracket
                            instructions.Add("</term>");
                        }
                    }
                    else
                    {
                        instructions.Add($"<term kind=\"variable\" subroutine=\"{_currentExecutingSubroutine}\">");
                        instructions.Add(ToXmlElement(token));
                        instructions.Add("</term>");
                    }
                }
            }
            
            instructions.Add("</expression>");

            return instructions;
        }

        private IList<string> WriteExpression(XmlNode expressionTree)
        {
            var instructions = new List<string>();

            for (int i = 0; i < expressionTree.ChildNodes.Count;)
            {
                if (i == 0) // term
                {
                    var termNode = expressionTree.ChildNodes[i];

                    instructions.AddRange(WriteTerm(termNode));

                    i++;

                    continue;
                }
                else // op term
                {
                    // perform Reverse Polish Notation
                    var operatorNode = expressionTree.ChildNodes[i];
                    var nextTermNode = expressionTree.ChildNodes[i + 1];

                    instructions.AddRange(WriteTerm(nextTermNode));
                    instructions.Add(_vmWriter.WriteArithmetic(operatorNode));

                    i += 2;
                }
            }

            return instructions;
        }

        public IList<string> WriteTerm(XmlNode termNode)
        {
            var instructions = new List<string>();

            if (_vmWriter.IsInteger(termNode))
            {
                var termValue = _vmWriter.GetIntegerValue(termNode);

                instructions.AddRange(_vmWriter.WritePush("constant", termValue));
            }
            if (_vmWriter.IsThis(termNode))
            {
                instructions.AddRange(_vmWriter.WritePush("pointer", 0));
            }
            if (_vmWriter.IsBoolean(termNode))
            {
                var termValue = _vmWriter.GetBooleanValue(termNode);

                if (termValue)
                {
                    instructions.AddRange(_vmWriter.WritePush("constant", 0));
                    // true is represented as 111...n
                    instructions.Add("not"); 
                }
                else
                {
                    instructions.AddRange(_vmWriter.WritePush("constant", 0));
                }
            }
            else if (_vmWriter.IsExpression(termNode))
            {
                instructions.AddRange(WriteExpression(termNode.FirstChild));
            }
            else if (_vmWriter.IsNegation(termNode))
            {
                instructions.AddRange(WriteTerm(termNode.ChildNodes.Item(1)));
                instructions.Add(termNode.FirstChild.InnerText.Trim() == "-" ? "neg" : "not"); // - (arithmetic) or ~ (bitwise)
            }
            else if (_vmWriter.IsSubroutineCall(termNode))
            {
                //this subroutineName will be the fully qualified name i.e <class>.<subroutine>
                var subroutineName = termNode.FirstChild.InnerText;
                var expressionTreeList = termNode.ChildNodes[1];

                foreach (XmlNode expressionTree in expressionTreeList.ChildNodes)
                {
                    instructions.AddRange(WriteExpression(expressionTree));
                }

                instructions.AddRange(_vmWriter.WriteCall(subroutineName, expressionTreeList.ChildNodes.Count));

                if (IsUserDefinedSubroutine(subroutineName))
                {
                    _currentExecutingSubroutine = subroutineName;
                }
            }
            else if (_vmWriter.IsVariable(termNode))
            {
                var variableName = termNode.FirstChild.InnerText;
                var subroutineName = termNode.Attributes["subroutine"].Value;
                var symbol = _symbolTable.GetSymbolByName(variableName);

                instructions.AddRange(_vmWriter.WritePush(symbol.ToSegment(), symbol.RunningIndex));
            }

            return instructions;
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileVarDec(Stack<Token> tokens)
        {
            var instructions = new List<string>();
            var currentIndex = -1;
            var identifierType = string.Empty;
            var identifierNames = new List<string>();
            var lastTokenValue = string.Empty;

            while (lastTokenValue != ";")
            {
                currentIndex++;
                var token = tokens.Pop();

                if (currentIndex == 1)
                {
                    identifierType = token.Value;
                }
                else if (token.TokenType == TokenType.Identifier)
                {
                    identifierNames.Add(token.Value);
                }

                lastTokenValue = token.Value;
            }

            foreach (var identifierName in identifierNames)
            {
                _symbolTable.DefineIdentifier(identifierName, identifierType, "var");
            }

            //TODO: Determine the memory that needs to be allocated, for example when the type of the identifier
            //is an Object or a string

            return ("varDec", instructions);
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileReturn(Stack<Token> tokens)
        {
            var instructions = new List<string>();
            var lastTokenValue = string.Empty;

            while (lastTokenValue != ";")
            {
                if (lastTokenValue == "return")
                {
                    var expressionTokens = PopUntil(";", tokens);

                    if (expressionTokens.Count > 0)
                    {
                        var expression = CompileExpression(expressionTokens);
                        var expressionTree = ExpressionTree.ConvertToXmlDocument(expression).FirstChild;

                        instructions.AddRange(WriteExpression(expressionTree));
                    }
                }

                var token = tokens.Pop();

                lastTokenValue = token.Value;
            }

            instructions.Add("return");

            return ("returnStatement", instructions);
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileClassVarDec(Stack<Token> tokens)
        {
            var instructions = new List<string>();
            var lastTokenValue = string.Empty;
            var currentIndex = -1;
            var identifierType = string.Empty;
            var identifierNames = new List<string>();

            while (lastTokenValue != ";")
            {
                currentIndex++;
                var token = tokens.Pop();

                if (currentIndex == 1)
                {
                    identifierType = token.Value;
                }
                else if (token.TokenType == TokenType.Identifier)
                {
                    identifierNames.Add(token.Value);
                }

                lastTokenValue = token.Value;
            }

            foreach (var identifierName in identifierNames)
            {
                _symbolTable.DefineIdentifier(identifierName, identifierType, "field");
            }

            return ("classVarDec", instructions);
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileSubroutine(Stack<Token> tokens)
        {
            var instructions = new List<string>();
            var subroutineSignature = tokens.Pop(3);
            var subroutineReturnType = subroutineSignature[1].Value;
            var subroutineName = $"{_className}.{subroutineSignature[2].Value}";
            var parameterList = tokens.PopParameterList();
            var indexOfClosingBracket = FindIndexOfClosingBracket(0, tokens);
            var numberOfRemainingTokensAfterSubroutine = tokens.Count - (indexOfClosingBracket + 1);

            _userDefinedSubroutines.Add(subroutineName);

            //I was getting confused with runtime, design time and compile time. I thought that writing a "call" vm instruction would
            //perform the goto, and jump to the execution of the called subroutine and therefore losing knowledge of the stored variable names.
            //This is not true, because at compile time, it is merely writing call and I will still be contextually inside the subroutine, 
            //meaning I still have access to all the stored variables. This is why I can call StartSubroutine and reinitialise 
            //SymbolTable._subroutineScopeIdentifiers whenever compiling a subroutine.
            _symbolTable.StartSubroutine();

            //the main function is called by the runtime i.e. called by Sys.init
            //therefore, as soon as the subroutine is compiled I can set it as the current executing subroutine
            if (subroutineName == "Main.main")
            {
                _currentExecutingSubroutine = subroutineName;
            }

            for (int i = 0, j = 1; i < parameterList.Count; i += 2, j += 2)
            {
                var parameterType = parameterList[i].Value;
                var parameterName = parameterList[j].Value;

                _symbolTable.DefineIdentifier(parameterName, parameterType, "argument");
            }

            var openingBracketToken = tokens.Pop(); // pop opening bracket
            var varDeclarations = new List<string>();
            var statementDeclarations = new List<string>();
            IList<string> returnStatement = null;

            //continue to pop tokens that are defined within this subroutine
            //the count should be minus 1 because we handle popping off the closing }
            //manually
            while ((tokens.Count - 1) > numberOfRemainingTokensAfterSubroutine)
            {
                var (constructType, constructInstructions) = CompileNextToken(tokens);

                if (constructType == "varDec")
                {
                    varDeclarations.AddRange(constructInstructions);
                }
                else if (constructType == "returnStatement")
                {
                    returnStatement = constructInstructions;
                }
                else if (constructType.Contains("statement", StringComparison.InvariantCultureIgnoreCase))
                {
                    statementDeclarations.AddRange(constructInstructions);
                }
            }

            tokens.Pop(); // pop closing bracket

            instructions.AddRange(_vmWriter.WriteFunction(subroutineName, _symbolTable.VarCount("var")));
            instructions.AddRange(varDeclarations);
            instructions.AddRange(statementDeclarations);

            if (subroutineReturnType == "void")
            {
                instructions.AddRange(_vmWriter.WritePush("constant", 0));
            }

            if (returnStatement != null)
            {
                instructions.AddRange(returnStatement);
            }

            return ("subroutineDec", instructions);
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileClass(Stack<Token> tokens)
        {
            var instructions = new List<string>();
            var classSignature = tokens.Pop(3);
            _className = classSignature.First(x => x.TokenType == TokenType.Identifier).Value;

            while (tokens.Count > 1)
            {
                (string constructType, IList<string> constructionInstructions) = CompileNextToken(tokens);

                instructions.AddRange(constructionInstructions);
            }

            var lastToken = tokens.Pop();

            return ("class", instructions);
        }

        private int FindIndexOfClosingBracket(int indexOfOpeningBracket, Stack<Token> tokens)
        {
            int subOpeningBrackets = 0;

            for (int i = indexOfOpeningBracket + 1; i < tokens.Count(); i++)
            {
                var token = tokens.ElementAt(i);

                if (token.Value == "{")
                {
                    subOpeningBrackets++;
                }
                else if (token.Value == "}")
                {
                    if (subOpeningBrackets == 0)
                    {
                        return i;
                    }

                    subOpeningBrackets--;
                }
            }

            throw new Exception("Closing bracket not found");
        }

        private string ToXmlElement(Token token)
        {
            return $"<{token.TokenType}>{XmlEncoder.EncodeTokenValue(token.Value)}</{token.TokenType}>";
        }

        private string ToXmlElement(string name, string value)
        {
            return $"<{name}>{XmlEncoder.EncodeTokenValue(value)}</{name}>";
        }

        private string ToXmlElement(Token token, string callingConstruct)
        {
            var openingTagContent = $"{token.TokenType}";

            if(token.TokenType == TokenType.Identifier)
            {
                if (!string.IsNullOrEmpty(callingConstruct))
                {
                    openingTagContent += $" category=\"{callingConstruct}\"";
                }

                if (!string.IsNullOrEmpty(callingConstruct) && Regex.IsMatch(callingConstruct, "(field|static|argument|var|let|expression)"))
                {
                    var isBeingDefined = Regex.IsMatch(callingConstruct, "(field|static|var)"); //else is being referenced
                    
                    openingTagContent += $" kind=\"{callingConstruct.Replace("let", "var")}\" isBeingDefined=\"{isBeingDefined}\"";
                }

                if (!string.IsNullOrEmpty(callingConstruct) && Regex.IsMatch(callingConstruct, "(field|static|argument|var)"))
                {
                    var runningIndex = _symbolTable.DefineIdentifier(token.Value, "", callingConstruct);

                    openingTagContent += $" runningIndex=\"{runningIndex}\"";
                }
            }

            return $"<{openingTagContent}> {XmlEncoder.EncodeTokenValue(token.Value)} </{token.TokenType}>";
        }

        private bool IsUserDefinedSubroutine(string subroutineName)
        {
            return _userDefinedSubroutines.Any(x => x == subroutineName);
        }
    }
}
