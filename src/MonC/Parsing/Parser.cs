using System;
using System.Collections.Generic;
using MonC.Parsing.ParseTreeLeaves;
using MonC.SyntaxTree;

namespace MonC
{
    public class Parser
    {   
        private readonly List<Token> _tokens = new List<Token>();
        private int _currentTokenIndex;

        private IList<ParseError> _errors;

        public void Parse(IEnumerable<Token> tokens, IList<IASTLeaf> tree, IList<ParseError> errors)
        {
            _tokens.Clear();
            _tokens.AddRange(tokens);

            _errors = errors;

            while (Peek().Type != TokenType.None) {
                tree.Add(ParseTopLevelStatement());
            }
        }

        private Token Peek(int offset = 0)
        {
            int i = _currentTokenIndex + offset;
            if (i >= _tokens.Count) {
                return new Token();
            }
            return _tokens[i];
        }

        private void Consume()
        {
            ++_currentTokenIndex;
        }

        private Token Next()
        {
            Token token = Peek();
            Consume();
            return token;
        }
        
        private bool Next(TokenType type, out Token token)
        {
            token = Next();

            if (token.Type != type) {
                _errors.Add(new ParseError {
                    Message = $"Expecting token of type {type}, got {token.Type}",
                    Token = token
                });
                return false;
            }

            return true;
        }

        private bool Next(TokenType type, string value, out Token token)
        {
            token = Next();
            
            if (token.Type != type || token.Value != value) {
                _errors.Add(new ParseError {
                    Message = $"Expecting token of type {type} with value {value}, got {token.Type} with value {token.Value}",
                    Token = token
                });
                return false;
            }

            return true;
        }

        private void AddError(string message, Token token)
        {
            _errors.Add(new ParseError {Message = message, Token = token});
        }

        private IASTLeaf ParseTopLevelStatement()
        {
            return ParseFunction();
        }
        
        private IASTLeaf ParseFunction()
        {
            var parameters = new List<DeclarationLeaf>();

            Token returnType, name;
            if (!(
                Next(TokenType.Identifier, out returnType) 
                && Next(TokenType.Identifier, out name)
                && Next(TokenType.Syntax, "(", out _)
            )) {
                return null;
            }
            
            bool expectingEnd = true;
            bool expectingComma = false;
            bool expectingNext = true;
            
            while (true) {
                if (expectingNext) {
                    Token paramType, paramName;
                    paramType = Peek();
                    paramName = Peek(1);

                    if (paramType.Type == TokenType.Identifier && paramName.Type == TokenType.Identifier) {
                        parameters.Add(new DeclarationLeaf(paramType.Value, paramName.Value, null));
                        Next();
                        Next();
                        expectingNext = false;
                        expectingComma = true;
                        expectingEnd = true;
                        continue;
                    }
                }

                if (expectingComma) {
                    Token comma = Peek();
                    if (comma.Type == TokenType.Syntax && comma.Value == ",") {
                        Next();
                        expectingNext = true;
                        expectingComma = false;
                        expectingEnd = false;
                        continue;
                    }
                }

                if (expectingEnd) {
                    Token rightParen = Peek();
                    if (rightParen.Type == TokenType.Syntax && rightParen.Value == ")") {
                        Next();
                        break;
                    }
                }
                
                AddError("Unexpected token", Peek());
                return null;
            }

            IASTLeaf body = ParseBody();
            return new FunctionDefinitionLeaf(name.Value, returnType.Value, parameters, body);    
        }

        private IASTLeaf ParseBody()
        {
            if (!Next(TokenType.Syntax, "{", out _)) {
                return null;
            }
            
            List<IASTLeaf> statements = new List<IASTLeaf>();
            
            while (true) {
                statements.Add(ParseStatement());

                Token next = Peek();

                if (next.Type == TokenType.None) {
                    AddError("Expected end of body", next);
                    break;
                }
                
                if (next.Type == TokenType.Syntax && next.Value == "}") {
                    Consume();
                    break;
                }
            }
            
            return new BodyLeaf(statements);
        }

        private IASTLeaf ParseStatement()
        {
            if (CheckDeclaration()) {
                IASTLeaf declaration = ParseDeclaration();
                ParseSemicolon();
                return declaration;
            }
            if (CheckFlow()) {
                return ParseFlow();
            }

            IASTLeaf expression = ParseExpression();
            ParseSemicolon();
            return expression;
        }

        private bool CheckDeclaration()
        {
            Token typeToken = Peek();
            Token nameToken = Peek(1);
            
            return 
                typeToken.Type == TokenType.Identifier
                && nameToken.Type == TokenType.Identifier;
        }

        private bool CheckFlow()
        {
            Token token = Peek();
            
            if (token.Type != TokenType.Keyword) {
                return false;
            }
            
            return token.Value == Keyword.IF || token.Value == Keyword.WHILE || token.Value == Keyword.FOR;
        }

        private IASTLeaf ParseDeclaration()
        {
            Token typeToken, nameToken;
            if (!(Next(TokenType.Identifier, out typeToken) && Next(TokenType.Identifier, out nameToken))) {
                return null;
            }

            IASTLeaf assignment = null;

            Token nextToken = Peek();
            
            if (nextToken.Type == TokenType.Syntax && nextToken.Value == "=") {
                Consume();
                assignment = ParseExpression();
            }

            return new DeclarationLeaf(typeToken.Value, nameToken.Value, assignment);
        }

        private IASTLeaf ParseFlow()
        {
            Token token = Peek();

            if (token.Type != TokenType.Keyword) {
                AddError("Expecting keyword", token);
                return null;
            }

            switch (token.Value) {
                case Keyword.IF:
                    return ParseIfElse();
                case Keyword.WHILE:
                    return ParseWhile();
                case Keyword.FOR:
                    return ParseFor();
            }
            
            AddError("Unexpected token", token);
            return null;
        }

        private IASTLeaf ParseIfElse()
        {
            if (!(Next(TokenType.Keyword, Keyword.IF, out _) && Next(TokenType.Syntax, "(", out _))) {
                return null;
            }

            IASTLeaf condition = ParseExpression();

            if (!Next(TokenType.Syntax, ")", out _)) {
                return null;
            }

            IASTLeaf ifBody = ParseBody();
            IASTLeaf elseBody = null;

            Token nextToken = Peek();

            if (nextToken.Type == TokenType.Keyword && nextToken.Value == Keyword.ELSE) {
                Consume();
                elseBody = ParseBody();
            }
            
            return new IfElseLeaf(condition, ifBody, elseBody);                
        }

        private IASTLeaf ParseWhile()
        {
            if (!(Next(TokenType.Keyword, Keyword.WHILE, out _) && Next(TokenType.Syntax, "(", out _))) {
                return null;
            }

            IASTLeaf condition = ParseExpression();

            if (!Next(TokenType.Syntax, ")", out _)) {
                return null;
            }

            IASTLeaf body = ParseBody();
            
            return new WhileLeaf(condition, body);
        }

        private IASTLeaf ParseFor()
        {
            if (!(Next(TokenType.Keyword, Keyword.FOR, out _) && Next(TokenType.Syntax, "(", out _))) {
                return null;
            }
            
            IASTLeaf declaration = ParseDeclaration();
            ParseSemicolon();
            IASTLeaf condition = ParseExpression();
            ParseSemicolon();
            IASTLeaf update = ParseExpression();

            if (!Next(TokenType.Syntax, ")", out _)) {
                return null;
            }
            
            IASTLeaf body = ParseBody();
            return new ForLeaf(declaration, condition, update, body);
        }

        private IASTLeaf ParseExpression()
        {
            IASTLeaf primary = ParsePrimaryExpression();
            return ParseOperator(primary, -1);       
        }

        private IASTLeaf ParseOperator(IASTLeaf lhs, int precedence)
        {
            while (true) {
                Token tok = Peek();
                int rhsPrecedence = GetTokenPrecedence(tok);

                if (rhsPrecedence == -1) {
                    return lhs;
                }
                
                if (precedence > rhsPrecedence) {
                    return lhs;
                }
                
                if (tok.Value == "(") {
                    lhs = ParseFunctionCall(lhs);
                    continue;
                }
                
                
                // Eat the operator
                Consume();

                IASTLeaf rhs = ParsePrimaryExpression();
                if (rhs == null) {
                    // Failed to parse primary expression. abort.
                    return null;
                }
                
                Token nextToken = Peek();
                int nextPrecedence = GetTokenPrecedence(nextToken);

                if (nextPrecedence > rhsPrecedence) {
                    rhs = ParseOperator(rhs, rhsPrecedence + 1);
                }
                
                lhs = new BinaryOperationExpressionLeaf(lhs, rhs, tok);
            }
        }

        private int GetTokenPrecedence(Token token)
        {
            if (token.Type != TokenType.Syntax) {
                return -1;
            }

            switch (token.Value) {
                case "(":
                    return 3;
                case "*":
                case "/":
                case "%":
                    return 2;
                case "+":
                case "-":
                    return 1;
                case "<":
                case ">":
                case ">=":
                case "<=":
                case "==":
                case "=":
                    return 0;
                default: 
                    return -1;
            }
        }

        private IASTLeaf ParseFunctionCall(IASTLeaf lhs)
        {   
            // Eat left paren
            if (!Next(TokenType.Syntax, "(", out _)) {
                return null;
            }
            
            List<IASTLeaf> arguments = new List<IASTLeaf>();
            
            bool expectingNextArgument = true;
            bool closingParensAllowed = true;

            while (true) {
                Token token = Peek();
                
                if (token.Type == TokenType.Syntax && token.Value == ")") {
                    if (!closingParensAllowed) {
                        AddError("Unexpected closing parenthesis", token);
                    }
                    Consume();
                    break;
                }
                
                if (!expectingNextArgument) {
                    AddError("Expecting closing parenthesis", token);
                    break;
                }

                IASTLeaf argument = ParseExpression();
                arguments.Add(argument);

                token = Peek();
                if (token.Type == TokenType.Syntax && token.Value == ",") {
                    Consume();
                    closingParensAllowed = false;

                } else {
                    closingParensAllowed = true;
                    expectingNextArgument = false;
                }
            }
            
            return new FunctionCallParseLeaf(lhs, arguments);
        }

        private IASTLeaf ParsePrimaryExpression()
        {
            Token token = Peek();

            if (token.Type == TokenType.Syntax && token.Value == "(") {
                return ParseParenthesisExpression();
            }

            if (token.Type == TokenType.Identifier) {
                return ParseIdentifierExpression();
            }

            if (token.Type == TokenType.Number) {
                return ParseNumericLiteralExpression();
            }

            if (token.Type == TokenType.String) {
                return ParseStringLiteralExpression();
            }
            
            AddError("Unexpected token while parsing primary expression", token);
            return null;
        }

        private IASTLeaf ParseParenthesisExpression()
        {
            if (!Next(TokenType.Syntax, "(", out _)) {
                return null;
            }
            
            IASTLeaf expression = ParseExpression();

            Next(TokenType.Syntax, ")", out _);
            return expression;
        }

        private IASTLeaf ParseIdentifierExpression()
        {
            Token token;
            Next(TokenType.Identifier, out token);
            return new IdentifierParseLeaf(token.Value);
        }
        
        private IASTLeaf ParseNumericLiteralExpression()
        {
            Token token;
            Next(TokenType.Number, out token);
            return new NumericLiteralLeaf(token.Value);
        }
        
        private IASTLeaf ParseStringLiteralExpression()
        {
            Token token;
            Next(TokenType.String, out token);
            return new StringLiteralLeaf(token.Value);
        }

        private void ParseSemicolon()
        {
            Next(TokenType.Syntax, ";", out _);
        }

    }
}