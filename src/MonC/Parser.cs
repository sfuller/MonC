using System;
using System.Collections.Generic;
using System.Linq;
using MonC.SyntaxTree;

namespace MonC
{
    public class Parser
    {
           
        private readonly List<Token> _tokens = new List<Token>();
        private int _currentTokenIndex;

        private IList<string> _errors;

        public void Parse(IEnumerable<Token> tokens, IList<IASTLeaf> tree, IList<string> errors)
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

        private Token Next(TokenType type)
        {
            Token token = Next();

            if (token.Type != type) {
                throw new ParseException();
            }
            
            return token;
        }

        private Token Next(TokenType type, string value)
        {
            Token token = Next(type);
            
            if (token.Value != value) {
                throw new ParseException();
            }
            
            return token;
        }

        private void AddError(ParseException e)
        {
            _errors.Add("TODO");
        }

        private IASTLeaf ParseTopLevelStatement()
        {
            return ParseFunction();
        }
        
        private FunctionLeaf ParseFunction()
        {
            try {
                var parameters = new List<FunctionLeaf.Parameter>();

                Token returnType = Next(TokenType.Identifier);
                Token name = Next(TokenType.Identifier);
                Next(TokenType.Syntax, "(");

                Token next = Next();

                if (!(next.Type == TokenType.Syntax && next.Value == ")")) {
                    // Parse parameters
                    while (true) {
                        Token paramType = Next(TokenType.Identifier);
                        Token paramName = Next(TokenType.Identifier);

                        parameters.Add(new FunctionLeaf.Parameter {Name = paramName.Value, Type = paramType.Value});

                        Token nextParamToken = Next(TokenType.Syntax);

                        if (nextParamToken.Value == ")") {
                            // No more parameters.
                            break;
                        }

                        // if next token isn't a closing paren, expect comma.
                        if (nextParamToken.Value != ",") {
                            throw new ParseException();
                        }
                    }
                }

                List<IASTLeaf> statements = new List<IASTLeaf>();
                ParseBody(statements);

                return new FunctionLeaf(name.Value, returnType.Value, parameters, statements);    
            } 
            catch (ParseException e) {
                AddError(e);
                return new FunctionLeaf("", "", Enumerable.Empty<FunctionLeaf.Parameter>(), Enumerable.Empty<IASTLeaf>());
            }
        }

        private void ParseBody(IList<IASTLeaf> statements)
        {
            try {
                Next(TokenType.Syntax, "{");
                
                while (true) {
                    statements.Add(ParseStatement());

                    Token next = Peek();

                    if (next.Type == TokenType.Syntax && next.Value == "}") {
                        Consume();
                        break;
                    }
                }
            } 
            catch (ParseException e) {
                AddError(e);
            }
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
            try {
                Token typeToken = Next(TokenType.Identifier);
                Token nameToken = Next(TokenType.Identifier);

                IASTLeaf assignment = null;

                Token nextToken = Peek();
                
                if (nextToken.Type == TokenType.Syntax && nextToken.Value == "=") {
                    Consume();
                    assignment = ParseExpression();
                }

                return new DeclarationLeaf(typeToken.Value, nameToken.Value, assignment);
            } 
            catch (ParseException e) {
                AddError(e);
                return new DeclarationLeaf("", "", null);
            }
        }

        private IASTLeaf ParseFlow()
        {
            Token token = Peek();

            if (token.Type != TokenType.Keyword) {
                AddError(new ParseException());
                return new PlaceholderLeaf();
            }

            switch (token.Value) {
                case Keyword.IF:
                    return ParseIfElse();
                case Keyword.WHILE:
                    return ParseWhile();
                case Keyword.FOR:
                    return ParseFor();
            }
            
            AddError(new ParseException());
            return new PlaceholderLeaf();
        }

        private IASTLeaf ParseIfElse()
        {
            try {
                Next(TokenType.Keyword, Keyword.IF);
                Next(TokenType.Syntax, "(");

                IASTLeaf condition = ParseExpression();

                Next(TokenType.Syntax, ")");
                
                List<IASTLeaf> ifBody = new List<IASTLeaf>();
                List<IASTLeaf> elseBody = new List<IASTLeaf>();
                
                ParseBody(ifBody);

                Token nextToken = Peek();

                if (nextToken.Type == TokenType.Keyword && nextToken.Value == Keyword.ELSE) {
                    Consume();
                    ParseBody(elseBody);
                }
                
                return new IfElseLeaf(condition, ifBody, elseBody);                
            }
            catch (ParseException e) {
                AddError(e);
                return new PlaceholderLeaf();
            }
        }

        private IASTLeaf ParseWhile()
        {
            try {
                Next(TokenType.Keyword, Keyword.WHILE);
                Next(TokenType.Syntax, "(");

                IASTLeaf condition = ParseExpression();

                Next(TokenType.Syntax, ")");

                List<IASTLeaf> body = new List<IASTLeaf>();
                
                ParseBody(body);

                return new WhileLeaf(condition, body);
            } 
            catch (ParseException e) {
                AddError(e);
                return new PlaceholderLeaf();
            }
        }

        private IASTLeaf ParseFor()
        {
            try {
                Next(TokenType.Keyword, Keyword.FOR);
                Next(TokenType.Syntax, "(");
                IASTLeaf declaration = ParseDeclaration();
                ParseSemicolon();
                IASTLeaf condition = ParseExpression();
                ParseSemicolon();
                IASTLeaf update = ParseExpression();
                Next(TokenType.Syntax, ")");

                List<IASTLeaf> body = new List<IASTLeaf>();
                
                ParseBody(body);

                return new ForLeaf(declaration, condition, update, body);
            } 
            catch (ParseException e) {
                AddError(e);
                return new PlaceholderLeaf();
            }
        }

        private IASTLeaf ParseExpression()
        {
            IASTLeaf primary = ParsePrimaryExpression();
            return ParseExpressionRHS(primary, -1);       
        }

        private IASTLeaf ParseExpressionRHS(IASTLeaf lhs, int precedence)
        {
            while (true) {
                Token tok = Peek();
                int rhsPrecedence = GetTokenPrecedence(tok);

                if (precedence > rhsPrecedence) {
                    return lhs;
                }

                // Eat the binary operator
                Consume();

                IASTLeaf rhs = ParsePrimaryExpression();
                if (rhs == null) {
                    // Failed to parse primary expression. abort.
                    return null;
                }
                
                Token nextToken = Peek();
                int nextPrecedence = GetTokenPrecedence(nextToken);

                if (nextPrecedence > rhsPrecedence) {
                    rhs = ParseExpressionRHS(rhs, rhsPrecedence + 1);
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

        private IASTLeaf ParsePrimaryExpression()
        {
            try {
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
                
                throw new ParseException();
            } 
            catch (ParseException e) {
                AddError(e);
                return null;
            }
        }

        private IASTLeaf ParseParenthesisExpression()
        {
            try {
                Next(TokenType.Syntax, "(");
                IASTLeaf expression = ParseExpression();
                Next(TokenType.Syntax, ")");
                return expression;
            }
            catch (ParseException e) {
                AddError(e);    
                return new PlaceholderLeaf();
            }
        }

        private IASTLeaf ParseIdentifierExpression()
        {
            try {
                Token token = Next(TokenType.Identifier);
                return new IdentifierLeaf(token.Value);
            } catch (ParseException e) {
                AddError(e);
                return new IdentifierLeaf(""); 
            }
        }
        
        private IASTLeaf ParseNumericLiteralExpression()
        {
            try {
                Token token = Next(TokenType.Number);
                return new NumericLiteralLeaf(token.Value);
            } catch (ParseException e) {
                AddError(e);
                return new NumericLiteralLeaf(""); 
            }
        }
        
        private IASTLeaf ParseStringLiteralExpression()
        {
            try {
                Token token = Next(TokenType.String);
                return new StringLIteralLeaf(token.Value);
            } catch (ParseException e) {
                AddError(e);
                return new StringLIteralLeaf(""); 
            }
        }

        private void ParseSemicolon()
        {
            try {
                Next(TokenType.Syntax, ";");
            } catch (ParseException e) {
                AddError(e);
            }
        }


    }
}