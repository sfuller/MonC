using System;
using System.Collections.Generic;
using System.Globalization;
using MonC.Parsing;
using MonC.Parsing.ParseTreeLeaves;
using MonC.Parsing.Semantics;
using MonC.SyntaxTree;

namespace MonC
{
    public class Parser
    {   
        private readonly List<Token> _tokens = new List<Token>();
        private int _currentTokenIndex;

        private IList<ParseError> _errors;
        
        public void Parse(IEnumerable<Token> tokens, Module module, IList<ParseError> errors, IList<FunctionDefinitionLeaf> functions)
        {
            _tokens.Clear();
            _tokens.AddRange(tokens);

            _errors = errors;

            IList<FunctionDefinitionLeaf> newFunctions = new List<FunctionDefinitionLeaf>();
            
            while (Peek().Type != TokenType.None) {
                ParseTopLevelStatement(newFunctions, module.Enums);
            }
            
            module.Functions.AddRange(newFunctions);

            SemanticAnalyzer analyzer = new SemanticAnalyzer();
            analyzer.AnalyzeModule(module, errors, functions);
            
            foreach (FunctionDefinitionLeaf func in newFunctions) {
                functions.Add(func);
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

        private IASTLeaf ParseTopLevelStatement(IList<FunctionDefinitionLeaf> functions, IList<EnumLeaf> enums)
        {
            bool isExported = true;

            Token token = Peek();
            if (token.Type == TokenType.Keyword && token.Value == Keyword.STATIC) {
                isExported = false;
                Next();
                token = Peek();
            }

            if (token.Type == TokenType.Keyword && token.Value == Keyword.ENUM) {
                EnumLeaf enumLeaf;
                if (ParseEnum(isExported).Get(out enumLeaf)) {
                    enums.Add(enumLeaf);
                    return enumLeaf;
                } 
                return new PlaceholderLeaf();
            }

            FunctionDefinitionLeaf def;
            if (ParseFunction(isExported).Get(out def)) {
                functions.Add(def);
                return def;
            }
            return new PlaceholderLeaf();
        }

        private Optional<EnumLeaf> ParseEnum(bool isExported)
        {
            if (!(
                Next(TokenType.Keyword, Keyword.ENUM, out _)
                && Next(TokenType.Syntax, "{", out _)
            )) {
                return new Optional<EnumLeaf>();
            }

            List<string> names = new List<string>();

            bool endIsAllowed = true;
            bool nextEnumerationIsAllowed = true;
            
            while (true) {
                Token next = Peek();

                if (next.Type == TokenType.None) {
                    AddError("Unexpected EOF", next);
                    break;
                }
                
                if (next.Type == TokenType.Syntax && next.Value == "}") {
                    if (!endIsAllowed) {
                        AddError("Expecting next enumeration", next);
                    }
                    Next();
                    break;
                }
                
                if (!nextEnumerationIsAllowed) {
                    AddError("Expecting }", next);
                    break;
                }

                Token name;
                if (!Next(TokenType.Identifier, out name)) {
                    break;
                }
                
                names.Add(name.Value);

                next = Peek();
                if (next.Type == TokenType.Syntax && next.Value == ",") {
                    endIsAllowed = false;
                    Next();
                } else {
                    endIsAllowed = true;
                    nextEnumerationIsAllowed = false;
                }
            }
            
            return new Optional<EnumLeaf>(new EnumLeaf(names, isExported));
        }
        
        private Optional<FunctionDefinitionLeaf> ParseFunction(bool isExported)
        {
            var parameters = new List<DeclarationLeaf>();

            Token returnType, name;
            if (!(
                Next(TokenType.Identifier, out returnType) 
                && Next(TokenType.Identifier, out name)
                && Next(TokenType.Syntax, "(", out _)
            )) {
                return new Optional<FunctionDefinitionLeaf>();
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
                        parameters.Add(new DeclarationLeaf(paramType.Value, paramName.Value, new Optional<IASTLeaf>(), paramType));
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
                return new Optional<FunctionDefinitionLeaf>();
            }

            IASTLeaf body = ParseBody();
            return new Optional<FunctionDefinitionLeaf>(new FunctionDefinitionLeaf(name.Value, returnType.Value, parameters, body, isExported));    
        }

        private IASTLeaf ParseBody()
        {
            Token bodyOpening;
            if (!Next(TokenType.Syntax, "{", out bodyOpening)) {
                return new PlaceholderLeaf();
            }
            
            List<IASTLeaf> statements = new List<IASTLeaf>();
            
            while (true) {
                Token next = Peek();
                
                if (next.Type == TokenType.Syntax && next.Value == "}") {
                    Consume();
                    break;
                }
                
                statements.Add(ParseStatement());

                next = Peek();

                if (next.Type == TokenType.None) {
                    AddError("Expected end of body", next);
                    break;
                }
            }
            
            return new BodyLeaf(statements, bodyOpening);
        }

        private IASTLeaf ParseStatement()
        {
            IASTLeaf statement;
            
            if (CheckDeclaration()) {
                statement = ParseDeclaration();
                ParseSemicolon();
            }
            else if (CheckFlow()) {
                statement = ParseFlow();
            } 
            else {
                statement = ParseExpression();
                ParseSemicolon();
            }

            return statement;
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
            
            return token.Value == Keyword.IF 
                || token.Value == Keyword.WHILE
                || token.Value == Keyword.FOR
                || token.Value == Keyword.RETURN;
        }

        private DeclarationLeaf ParseDeclaration()
        {
            Token typeToken, nameToken;
            if (!(Next(TokenType.Identifier, out typeToken) && Next(TokenType.Identifier, out nameToken))) {
                return new DeclarationLeaf("error", "error", new Optional<IASTLeaf>(), typeToken);
            }

            Optional<IASTLeaf> assignment = new Optional<IASTLeaf>();

            Token nextToken = Peek();
            
            if (nextToken.Type == TokenType.Syntax && nextToken.Value == "=") {
                Consume();
                assignment = new Optional<IASTLeaf>(ParseExpression());
            }

            return new DeclarationLeaf(typeToken.Value, nameToken.Value, assignment, typeToken);
        }

        private IASTLeaf ParseFlow()
        {
            Token token = Peek();

            if (token.Type != TokenType.Keyword) {
                AddError("Expecting keyword", token);
                return new PlaceholderLeaf();
            }

            switch (token.Value) {
                case Keyword.IF:
                    return ParseIfElse();
                case Keyword.WHILE:
                    return ParseWhile();
                case Keyword.FOR:
                    return ParseFor();
                case Keyword.RETURN:
                    return ParseReturn();
            }
            
            AddError("Unexpected token", token);
            return new PlaceholderLeaf();
        }

        private IASTLeaf ParseIfElse()
        {
            if (!(Next(TokenType.Keyword, Keyword.IF, out _) && Next(TokenType.Syntax, "(", out _))) {
                return new PlaceholderLeaf();
            }

            IASTLeaf condition = ParseExpression();

            if (!Next(TokenType.Syntax, ")", out _)) {
                return new PlaceholderLeaf();
            }

            IASTLeaf ifBody = ParseBody();
            Optional<IASTLeaf> elseBody = new Optional<IASTLeaf>();

            Token nextToken = Peek();

            if (nextToken.Type == TokenType.Keyword && nextToken.Value == Keyword.ELSE) {
                Consume();
                elseBody = new Optional<IASTLeaf>(ParseBody());
            }
            
            return new IfElseLeaf(condition, ifBody, elseBody);                
        }

        private IASTLeaf ParseWhile()
        {
            if (!(Next(TokenType.Keyword, Keyword.WHILE, out _) && Next(TokenType.Syntax, "(", out _))) {
                return new PlaceholderLeaf();
            }

            IASTLeaf condition = ParseExpression();

            if (!Next(TokenType.Syntax, ")", out _)) {
                return new PlaceholderLeaf();
            }

            IASTLeaf body = ParseBody();
            
            return new WhileLeaf(condition, body);
        }

        private IASTLeaf ParseFor()
        {
            if (!(Next(TokenType.Keyword, Keyword.FOR, out _) && Next(TokenType.Syntax, "(", out _))) {
                return new PlaceholderLeaf();
            }
            
            IASTLeaf declaration = ParseDeclaration();
            ParseSemicolon();
            IASTLeaf condition = ParseExpression();
            ParseSemicolon();
            IASTLeaf update = ParseExpression();

            if (!Next(TokenType.Syntax, ")", out _)) {
                return new PlaceholderLeaf();
            }
            
            IASTLeaf body = ParseBody();
            return new ForLeaf(declaration, condition, update, body);
        }

        private IASTLeaf ParseReturn()
        {
            // Eat 'return'
            Next();

            Token token = Peek();

            Optional<IASTLeaf> expression;
            
            if (token.Type == TokenType.Syntax && token.Value == ";") {
                expression = new Optional<IASTLeaf>();
            } else {
                expression = new Optional<IASTLeaf>(ParseExpression());
            }
            
            ParseSemicolon();

            return new ReturnLeaf {RHS = expression};
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
//                if (rhs == null) {
//                    // Failed to parse primary expression. abort.
//                    return null;
//                }
                
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
                    return 7;
                case "*":
                case "/":
                case "%":
                    return 6;
                case "+":
                case "-":
                    return 5;
                case "<":
                case ">":
                case Syntax.GREATER_THAN_OR_EQUAL_TO:
                case Syntax.LESS_THAN_OR_EQUAL_TO:
                    return 4;
                case Syntax.EQUALS:
                case Syntax.NOT_EQUALS:
                    return 3;
                case Syntax.LOGICAL_AND:
                    return 2;
                case Syntax.LOGICAL_OR:
                    return 1;
                case "=":
                    return 0;
                default: 
                    return -1;
            }
        }
        
        private IASTLeaf ParseFunctionCall(IASTLeaf lhs)
        {
            Token leftParen;
            
            // Eat left paren
            if (!Next(TokenType.Syntax, "(", out leftParen)) {
                return new PlaceholderLeaf();
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
            
            return new FunctionCallParseLeaf(lhs, arguments, leftParen);
        }

        private IASTLeaf ParsePrimaryExpression()
        {
            Token token = Peek();

            // Unary Operators
            if (token.Type == TokenType.Syntax) {
                if (token.Value == "(") {
                    return ParseParenthesisExpression();    
                }
                if (token.Value == "-") {
                    return ParseNegateOperator();
                }
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
            return new PlaceholderLeaf();
        }

        private IASTLeaf ParseParenthesisExpression()
        {
            if (!Next(TokenType.Syntax, "(", out _)) {
                return new PlaceholderLeaf();
            }
            
            IASTLeaf expression = ParseExpression();

            Next(TokenType.Syntax, ")", out _);
            return expression;
        }

        private IASTLeaf ParseNegateOperator()
        {
            Token op = Next();
            return new UnaryOperationLeaf(op, ParseExpression());
        }

        private IASTLeaf ParseIdentifierExpression()
        {
            Token token;
            Next(TokenType.Identifier, out token);
            return new IdentifierParseLeaf(token.Value, token);
        }
        
        private NumericLiteralLeaf ParseNumericLiteralExpression()
        {
            Token token;
            Next(TokenType.Number, out token);

            bool parseSuccess;
            int value;
            
            if (token.Value.StartsWith("0x")) {
                parseSuccess = int.TryParse(token.Value.Substring(2), NumberStyles.AllowHexSpecifier, null, out value);
            } else {
                parseSuccess = int.TryParse(token.Value, out value);
            }

            if (!parseSuccess) {
                AddError("Invalid numeric literal", token);
            }
            
            return new NumericLiteralLeaf(value);
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