﻿using JackCompiler.Net.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JackCompiler.Net
{
    public class CompilationEngine
    {
        public IList<string> Compile(IEnumerable<Token> tokens)
        {
            var tokenStack = new Stack<Token>(tokens.Reverse());

            var instructions = new List<string>();

            var construct = CompileNextToken(tokenStack);

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
            var instructions = new List<string>();

            instructions.Add("<ifStatement>");

            var lastTokenValue = string.Empty;

            while (tokens.Peek().Value != "{")
            {
                if (lastTokenValue == "(")
                {
                    var expressionTokens = PopExpressionTokensBetweenBrackets("(", ")", tokens);

                    instructions.AddRange(CompileExpression(expressionTokens));
                }

                var token = tokens.Pop();

                lastTokenValue = token.Value;
                instructions.Add(ToXmlElement(token));
            }

            var bodyTokens = PopStatementBody(tokens); //first token is "{", last token is "}"

            AddStatements(instructions, bodyTokens);

            var nextToken = tokens.Peek();

            if (nextToken.TokenType == "keyword" && nextToken.Value == "else")
            {
                var elseToken = tokens.Pop();

                instructions.Add(ToXmlElement(elseToken));

                var elseStatementBodyTokens = PopStatementBody(tokens);

                AddStatements(instructions, elseStatementBodyTokens);
            }

            instructions.Add("</ifStatement>");

            return ("ifStatement", instructions);
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileWhile(Stack<Token> tokens)
        {
            var instructions = new List<string>();

            instructions.Add("<whileStatement>");

            var lastTokenValue = string.Empty;

            while (tokens.Peek().Value != "{")
            {
                if (lastTokenValue == "(")
                {
                    var expressionTokens = PopExpressionTokensBetweenBrackets("(", ")", tokens);

                    instructions.AddRange(CompileExpression(expressionTokens));
                }

                var token = tokens.Pop();

                instructions.Add(ToXmlElement(token));
                lastTokenValue = token.Value;
            }

            var bodyTokens = PopStatementBody(tokens);

            AddStatements(instructions, bodyTokens);

            instructions.Add("</whileStatement>");

            return ("whileStatement", instructions);
        }

        private void AddStatements(List<string> instructions, Stack<Token> bodyTokens)
        {
            var openingBracketToken = bodyTokens.Pop();

            instructions.Add(ToXmlElement(openingBracketToken));

            instructions.Add("<statements>");

            while (bodyTokens.Count > 1)
            {
                var (constructType, constructInstructions) = CompileNextToken(bodyTokens);

                instructions.AddRange(constructInstructions);
            }

            instructions.Add("</statements>");

            var closingBracketToken = bodyTokens.Pop();

            instructions.Add(ToXmlElement(closingBracketToken));
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

            instructions.Add("<doStatement>");

            var lastTokenValue = string.Empty;

            while (lastTokenValue != ";")
            {
                if (lastTokenValue == "(")
                {
                    var expressionList = PopExpressionTokensBetweenBrackets("(", ")", tokens);

                    instructions.AddRange(CompileExpressionList(expressionList));
                }

                var token = tokens.Pop();

                instructions.Add(ToXmlElement(token));
                lastTokenValue = token.Value;
            }

            instructions.Add("</doStatement>");

            return ("doStatement", instructions);
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileLet(Stack<Token> tokens)
        {
            var instructions = new List<string>();

            instructions.Add("<letStatement>");

            var lastTokenValue = string.Empty;

            while (lastTokenValue != ";")
            {
                if (lastTokenValue == "[")
                {
                    var expressionTokens = PopExpressionTokensBetweenBrackets("[", "]", tokens);

                    instructions.AddRange(CompileExpression(expressionTokens));
                }
                else if (lastTokenValue == "=")
                {
                    var expressionTokens = PopUntil(";", tokens);

                    instructions.AddRange(CompileExpression(expressionTokens));
                }

                var token = tokens.Pop();

                instructions.Add(ToXmlElement(token));
                lastTokenValue = token.Value;
            }

            instructions.Add("</letStatement>");

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

                if (i < (expressions.Count - 1))
                {
                    instructions.Add(ToXmlElement(new Token { TokenType = TokenType.Symbol, Value = "," }));
                }
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
                        instructions.Add(ToXmlElement(token));

                        instructions.AddRange(CompileExpression(termExpression));

                        instructions.Add(ToXmlElement(expressionTokens.Pop()));
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
                            instructions.Add(ToXmlElement(expressionTokens.Pop())); // pop opening bracket

                            var innerExpressionTokens = PopExpressionTokensBetweenBrackets("(", ")", expressionTokens);

                            instructions.AddRange(CompileExpression(innerExpressionTokens));

                            instructions.Add(ToXmlElement(expressionTokens.Pop())); // pop closing bracket
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
                            instructions.Add(ToXmlElement(expressionTokens.Pop())); // pop opening bracket

                            var expressionList = PopExpressionTokensBetweenBrackets("(", ")", expressionTokens);

                            instructions.AddRange(CompileExpressionList(expressionList));

                            instructions.Add(ToXmlElement(expressionTokens.Pop())); // pop closing bracket
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
                        else // therefore ".", for example varName|className.subroutineName()
                        {
                            instructions.Add("<term>");
                            instructions.Add(ToXmlElement(token));
                            instructions.Add(ToXmlElement(expressionTokens.Pop())); // pop "."
                            instructions.Add(ToXmlElement(expressionTokens.Pop())); // pop subroutineName
                            instructions.Add(ToXmlElement(expressionTokens.Pop())); // pop opening bracket

                            var expressionList = PopExpressionTokensBetweenBrackets("(", ")", expressionTokens);
                            instructions.AddRange(CompileExpressionList(expressionList));

                            instructions.Add(ToXmlElement(expressionTokens.Pop())); // pop closing bracket
                            instructions.Add("</term>");
                        }
                    }
                    else
                    {
                        instructions.Add("<term>");
                        instructions.Add(ToXmlElement(token));
                        instructions.Add("</term>");
                    }
                }
            }
            
            instructions.Add("</expression>");

            return instructions;
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileVarDec(Stack<Token> tokens)
        {
            var instructions = new List<string>();

            instructions.Add("<varDec>");

            var lastTokenValue = string.Empty;

            while (lastTokenValue != ";")
            {
                var token = tokens.Pop();

                lastTokenValue = token.Value;

                instructions.Add(ToXmlElement(token));
            }

            instructions.Add("</varDec>");

            return ("varDec", instructions);
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileReturn(Stack<Token> tokens)
        {
            var instructions = new List<string>();

            instructions.Add("<returnStatement>");

            var lastTokenValue = string.Empty;

            while (lastTokenValue != ";")
            {
                if (lastTokenValue == "return")
                {
                    var expressionTokens = PopUntil(";", tokens);

                    if (expressionTokens.Count > 0)
                    {
                        instructions.AddRange(CompileExpression(expressionTokens));
                    }
                }

                var token = tokens.Pop();

                instructions.Add(ToXmlElement(token));
                lastTokenValue = token.Value;
            }

            instructions.Add("</returnStatement>");

            return ("returnStatement", instructions);
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileClassVarDec(Stack<Token> tokens)
        {
            var instructions = new List<string>();

            instructions.Add("<classVarDec>");

            var lastTokenValue = string.Empty;

            while (lastTokenValue != ";")
            {
                var token = tokens.Pop();

                lastTokenValue = token.Value;

                instructions.Add(ToXmlElement(token));
            }

            instructions.Add("</classVarDec>");

            return ("classVarDec", instructions);
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileSubroutine(Stack<Token> tokens)
        {
            var instructions = new List<string>();

            instructions.Add("<subroutineDec>");

            var subroutineSignature = tokens.Pop(3);
            var parameterList = tokens.PopParameterList();
            var indexOfClosingBracket = FindIndexOfClosingBracket(0, tokens);
            var numberOfRemainingTokensAfterSubroutine = tokens.Count - (indexOfClosingBracket + 1);

            foreach (var token in subroutineSignature)
            {
                instructions.Add(ToXmlElement(token));
            }

            foreach (var token in parameterList)
            {
                if (token.Value == ")")
                    instructions.Add("</parameterList>");

                instructions.Add(ToXmlElement(token));

                if (token.Value == "(")
                    instructions.Add("<parameterList>");
            }

            instructions.Add("<subroutineBody>");

            var openingBracketToken = tokens.Pop();

            instructions.Add(ToXmlElement(openingBracketToken));

            var varDeclarations = new List<string>();
            var statementDeclarations = new List<string>();

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
                else if (constructType.Contains("statement", StringComparison.InvariantCultureIgnoreCase))
                {
                    statementDeclarations.AddRange(constructInstructions);
                }
            }

            instructions.AddRange(varDeclarations);

            instructions.Add("<statements>");
            instructions.AddRange(statementDeclarations);
            instructions.Add("</statements>");

            var closingBracketToken = tokens.Pop();

            instructions.Add(ToXmlElement(closingBracketToken));

            instructions.Add("</subroutineBody>");

            instructions.Add("</subroutineDec>");

            return ("subroutineDec", instructions);
        }

        private (string ConstructType, IList<string> ConstructInstructions) CompileClass(Stack<Token> tokens)
        {
            var instructions = new List<string>();

            instructions.Add("<class>");

            var classSignature = tokens.Pop(3);

            foreach (var token in classSignature)
            {
                if (token.TokenType == TokenType.Identifier)
                {
                    instructions.Add(ToXmlElement(token, "class", null));
                }
                else
                {
                    instructions.Add(ToXmlElement(token));
                }
                
            }

            while (tokens.Count > 1)
            {
                (string constructType, IList<string> constructionInstructions) = CompileNextToken(tokens);

                instructions.AddRange(constructionInstructions);
            }

            var lastToken = tokens.Pop();

            instructions.Add(ToXmlElement(lastToken));

            instructions.Add("</class>");

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
            return $"<{token.TokenType}> {XmlEncoder.EncodeTokenValue(token.Value)} </{token.TokenType}>";
        }

        private string ToXmlElement(Token token, string category, string kind)
        {
            var openingTagContent = $"{token.TokenType}";

            if (!string.IsNullOrEmpty(category))
            {
                openingTagContent += $" category=\"{category}\"";
            }

            if (!string.IsNullOrEmpty(kind))
            {
                openingTagContent += $" kind=\"{kind}\"";
            }

            return $"<{openingTagContent}> {XmlEncoder.EncodeTokenValue(token.Value)} </{token.TokenType}>";
        }
    }
}
