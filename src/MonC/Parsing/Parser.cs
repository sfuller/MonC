using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MonC.Parsing;
using MonC.Parsing.ParseTreeLeaves;
using MonC.Parsing.Semantics;
using MonC.SyntaxTree;


namespace MonC
{
    public class Parser
    {   
        private readonly List<Token> _tokens = new List<Token>();
        private string? _filePath;

        private IDictionary<IASTLeaf, Symbol> _tokenMap = new Dictionary<IASTLeaf, Symbol>();
        
        public ParseModule Parse(string? filePath, IEnumerable<Token> tokens, ParseModule headerModule, IList<ParseError> errors)
        {
            _filePath = filePath;
            _tokens.Clear();
            _tokens.AddRange(tokens);

            TokenSource tokenSource = new TokenSource(_tokens, 0, errors);

            ParseModule outputModule = new ParseModule();
            _tokenMap = outputModule.TokenMap;
            
            while (tokenSource.Peek().Type != TokenType.None) {
                ParseTopLevelStatement(ref tokenSource, outputModule.Functions, outputModule.Enums);
            }
            
            SemanticAnalyzer analyzer = new SemanticAnalyzer(errors,_tokenMap);
            analyzer.Analyze(headerModule, outputModule);

            return outputModule;
        }

        private struct TokenSource
        {
            private readonly List<Token> _tokens;
            private int _offset;
            private IList<ParseError>? _errors;

            public TokenSource(List<Token> tokens, int offset, IList<ParseError>? errors)
            {
                _tokens = tokens;
                _offset = offset;
                _errors = errors;
            }
            
            public readonly Token Peek(int offset = 0)
            {
                int i = _offset + offset;
                if (i >= _tokens.Count) {
                    return new Token();
                }
                return _tokens[i];
            }
            
            public bool Peek(int offset, TokenType type, out Token token)
            {
                token = Peek(offset);
                if (token.Type != type) {
                    AddError(new ParseError {
                        Message = $"Expecting token of type {type}, got {token.Type}",
                        Start = token.Location,
                        End = token.DeriveEndLocation()
                    });
                    return false;
                }
                return true;
            }
            
            public bool Peek(int offset, TokenType type, string value, out Token token)
            {
                token = Peek(offset);
                if (token.Type != type || token.Value != value) {
                    AddError(new ParseError {
                        Message = $"Expecting token of type {type} with value {value}, got {token.Type} with value {token.Value}",
                        Start = token.Location,
                        End = token.DeriveEndLocation()
                    });
                    return false;
                }
                return true;
            }

            public void Consume(int count = 1)
            {
                _offset += count;
            }

            public Token Next()
            {
                Token token = Peek();
                Consume();
                return token;
            }
            
            public bool Next(TokenType type, out Token token)
            {
                bool result = Peek(0, type, out token);
                Consume();
                return result;
            }

            public bool Next(TokenType type, string value, out Token token)
            {
                bool result = Peek(0, type, value, out token);
                Consume();
                return result;
            }

            public void AddError(ParseError error)
            {
                if (_errors == null) {
                    _errors = new List<ParseError>();
                }
                _errors.Add(error);
            }

            public void AddError(string message, Token token)
            {
                AddError(new ParseError {
                    Message = message,
                    Start = token.Location,
                    End = token.DeriveEndLocation()
                });
            }

            public readonly TokenSource Fork()
            {
                return new TokenSource(_tokens, _offset, null);
            }

            public void Consume(in TokenSource other)
            {
                #if DEBUG
                if (_tokens != other._tokens) {
                    throw new Exception("Cannot call consume with a token source that uses different tokens.");
                }
                #endif
                
                _offset = other._offset;
                if (other._errors != null && _errors != null) {
                    foreach (ParseError error in other._errors) {
                        _errors.Add(error);
                    }
                }
            }
        }

        private void ParseTopLevelStatement(ref TokenSource tokens, IList<FunctionDefinitionLeaf> functions, IList<EnumLeaf> enums)
        {
            bool isExported = true;

            Token token = tokens.Peek();
            if (token.Type == TokenType.Keyword && token.Value == Keyword.STATIC) {
                isExported = false;
                tokens.Consume();
                token = tokens.Peek();
            }

            if (token.Type == TokenType.Keyword && token.Value == Keyword.ENUM) {
                EnumLeaf? enumLeaf = ParseEnum(ref tokens, isExported);
                if (enumLeaf != null) {
                    enums.Add(enumLeaf);
                }
                return;
            }

            FunctionDefinitionLeaf? def = ParseFunction(ref tokens, isExported);
            if (def != null) {
                functions.Add(def);
            }
        }

        private EnumLeaf? ParseEnum(ref TokenSource tokens, bool isExported)
        {
            Token startToken = tokens.Peek();
            
            if (!(
                tokens.Next(TokenType.Keyword, Keyword.ENUM, out _)
                && tokens.Next(TokenType.Syntax, "{", out _)
            )) {
                return null;
            }

            List<KeyValuePair<string, int>> enumerations = new List<KeyValuePair<string, int>>();

            bool endIsAllowed = true;
            bool nextEnumerationIsAllowed = true;
            
            while (true) {
                Token next = tokens.Peek();

                if (next.Type == TokenType.None) {
                    tokens.AddError("Unexpected EOF", next);
                    break;
                }
                
                if (next.Type == TokenType.Syntax && next.Value == "}") {
                    if (!endIsAllowed) {
                        tokens.AddError("Expecting next enumeration", next);
                    }
                    tokens.Next();
                    break;
                }
                
                if (!nextEnumerationIsAllowed) {
                    tokens.AddError("Expecting }", next);
                    break;
                }

                Token name;
                if (!tokens.Next(TokenType.Identifier, out name)) {
                    break;
                }
                
                enumerations.Add(new KeyValuePair<string, int>(name.Value, enumerations.Count));

                next = tokens.Peek();
                if (next.Type == TokenType.Syntax && next.Value == ",") {
                    endIsAllowed = false;
                    tokens.Next();
                } else {
                    endIsAllowed = true;
                    nextEnumerationIsAllowed = false;
                }
            }

            return NewLeaf(new EnumLeaf(enumerations, isExported), startToken, tokens.Peek());
        }
        
        private FunctionDefinitionLeaf? ParseFunction(ref TokenSource tokens, bool isExported)
        {
            var parameters = new List<DeclarationLeaf>();

            Token retunTypeStart = tokens.Peek();
            TypeSpecifierLeaf? returnType = ParseTypeSpecifier(ref tokens);
            if (returnType == null) {
                returnType = NewLeaf(new TypeSpecifierLeaf("", PointerType.NotAPointer), retunTypeStart, tokens.Peek());
            }

            Token name;
            if (!(
                tokens.Next(TokenType.Identifier, out name)
                && tokens.Next(TokenType.Syntax, "(", out _)
            )) {
                return null;
            }
            
            bool expectingEnd = true;
            bool expectingComma = false;
            bool expectingNext = true;
            
            while (true) {
                if (expectingNext) {
                    TokenSource declTokens = tokens.Fork();
                    DeclarationLeaf? decl = ParseDeclaration(ref declTokens);
                    if (decl != null) {
                        tokens.Consume(declTokens);
                        parameters.Add(decl);
                        expectingNext = false;
                        expectingComma = true;
                        expectingEnd = true;
                        continue;
                    }
                }

                if (expectingComma) {
                    Token comma = tokens.Peek();
                    if (comma.Type == TokenType.Syntax && comma.Value == ",") {
                        tokens.Consume();
                        expectingNext = true;
                        expectingComma = false;
                        expectingEnd = false;
                        continue;
                    }
                }

                if (expectingEnd) {
                    Token rightParen = tokens.Peek();
                    if (rightParen.Type == TokenType.Syntax && rightParen.Value == ")") {
                        tokens.Consume();
                        break;
                    }
                }
                
                tokens.AddError("Unexpected token", tokens.Peek());
                return null;
            }

            BodyLeaf? body = ParseBody(ref tokens);
            if (body == null) {
                return null;
            }

            return NewLeaf(
                new FunctionDefinitionLeaf(name.Value, returnType, parameters, body, isExported),
                returnType, tokens.Peek());
        }

        private BodyLeaf? ParseBody(ref TokenSource tokens)
        {
            Token bodyOpening;
            if (!tokens.Next(TokenType.Syntax, "{", out bodyOpening)) {
                return null;
            }
            
            List<IASTLeaf> statements = new List<IASTLeaf>();
            
            while (true) {
                Token next = tokens.Peek();
                
                if (next.Type == TokenType.Syntax && next.Value == "}") {
                    tokens.Consume();
                    break;
                }

                IASTLeaf? statement = ParseStatement(ref tokens);
                if (statement == null) {
                    return null;
                }
                statements.Add(statement);
            }
            
            return NewLeaf(new BodyLeaf(statements), bodyOpening, tokens.Peek());
        }

        private IASTLeaf? ParseStatement(ref TokenSource tokens)
        {

            IASTLeaf? TryLeafTypes(in TokenSource tokens, out TokenSource tokensToConsume)
            {
                tokensToConsume = tokens.Fork();
                IASTLeaf? statement = ParseDeclaration(ref tokensToConsume);
                if (statement != null) {
                    ParseSemiColonForgiving(ref tokensToConsume);
                    return statement;
                }

                tokensToConsume = tokens.Fork();
                statement = ParseFlow(ref tokensToConsume);
                if (statement != null) {
                    return statement;
                }

                tokensToConsume = tokens.Fork();
                statement = ParseExpression(ref tokensToConsume);
                ParseSemiColonForgiving(ref tokensToConsume);
                return statement;
            }

            IASTLeaf? statement = TryLeafTypes(in tokens, out TokenSource tokensToConsume);
            tokens.Consume(tokensToConsume);
            return statement;
        }

        private DeclarationLeaf? ParseDeclaration(ref TokenSource tokens)
        {
            Token startToken = tokens.Peek();
            TypeSpecifierLeaf? typeSpecifier = ParseTypeSpecifier(ref tokens);
            Token nameToken;
            if (typeSpecifier == null || !tokens.Next(TokenType.Identifier, out nameToken)) {
                return null;
            }
            
            IASTLeaf? assignment = null;

            Token nextToken = tokens.Peek();
            
            if (nextToken.Type == TokenType.Syntax && nextToken.Value == "=") {
                tokens.Consume();
                assignment = ParseExpression(ref tokens);
            }

            return NewLeaf(new DeclarationLeaf(typeSpecifier, nameToken.Value, assignment), startToken, nameToken);
        }

        private IASTLeaf? ParseFlow(ref TokenSource tokens)
        {
            Token token = tokens.Peek();

            if (token.Type != TokenType.Keyword) {
                tokens.AddError("Expecting keyword", token);
                return null;
            }

            switch (token.Value) {
                case Keyword.IF:
                    return ParseIfElse(ref tokens);
                case Keyword.WHILE:
                    return ParseWhile(ref tokens);
                case Keyword.FOR:
                    return ParseFor(ref tokens);
                case Keyword.RETURN:
                    return ParseReturn(ref tokens);
                case Keyword.CONTINUE:
                    return ParseContinue(ref tokens);
                case Keyword.BREAK:
                    return ParseBreak(ref tokens);
            }
            
            tokens.AddError("Unexpected token", token);
            return null;
        }

        private IfElseLeaf? ParseIfElse(ref TokenSource tokens)
        {
            Token ifToken;
            if (!(tokens.Next(TokenType.Keyword, Keyword.IF, out ifToken) && tokens.Next(TokenType.Syntax, "(", out _))) {
                return null;
            }

            IASTLeaf? condition = ParseExpression(ref tokens);
            if (condition == null) {
                return null;
            }

            if (!tokens.Next(TokenType.Syntax, ")", out _)) {
                return null;
            }

            BodyLeaf? ifBody = ParseBody(ref tokens);
            if (ifBody == null) {
                return null;
            }

            BodyLeaf? elseBody = null;
            Token nextToken = tokens.Peek();

            if (nextToken.Type == TokenType.Keyword && nextToken.Value == Keyword.ELSE) {
                tokens.Consume();
                elseBody = ParseBody(ref tokens);
            }
            
            return NewLeaf(new IfElseLeaf(condition, ifBody, elseBody), ifToken, tokens.Peek());
        }

        private WhileLeaf? ParseWhile(ref TokenSource tokens)
        {
            Token whileToken;
            if (!(tokens.Next(TokenType.Keyword, Keyword.WHILE, out whileToken) && tokens.Next(TokenType.Syntax, "(", out _))) {
                return null;
            }

            IASTLeaf? condition = ParseExpression(ref tokens);
            if (condition == null) {
                return null;
            }

            if (!tokens.Next(TokenType.Syntax, ")", out _)) {
                return null;
            }

            BodyLeaf? body = ParseBody(ref tokens);
            if (body == null) {
                return null;
            }

            return NewLeaf(new WhileLeaf(condition, body), whileToken, tokens.Peek());
        }

        private ForLeaf? ParseFor(ref TokenSource tokens)
        {
            Token forToken;
            if (!(tokens.Next(TokenType.Keyword, Keyword.FOR, out forToken) && tokens.Next(TokenType.Syntax, "(", out _))) {
                return null;
            }

            DeclarationLeaf? declaration = ParseDeclaration(ref tokens);
            if (declaration == null) {
                return null;
            }
            
            ParseSemiColonForgiving(ref tokens);

            IASTLeaf? condition = ParseExpression(ref tokens);
            if (condition == null) {
                return null;
            }
            
            ParseSemiColonForgiving(ref tokens);

            IASTLeaf? update = ParseExpression(ref tokens);
            if (update == null) {
                return null;
            }

            if (!tokens.Next(TokenType.Syntax, ")", out _)) {
                return null;
            }

            BodyLeaf? body = ParseBody(ref tokens);
            if (body == null) {
                return null;
            }

            return NewLeaf(new ForLeaf(declaration, condition, update, body), forToken, tokens.Peek());
        }

        private ReturnLeaf? ParseReturn(ref TokenSource tokens)
        {
            Token returnToken;
            tokens.Next(TokenType.Keyword, Keyword.RETURN, out returnToken);

            Token token = tokens.Peek();

            IASTLeaf? expression = null;
            
            if (!(token.Type == TokenType.Syntax && token.Value == ";")) {
                expression = ParseExpression(ref tokens);
            }
            
            ParseSemiColonForgiving(ref tokens);

            return NewLeaf(new ReturnLeaf {RHS = expression}, returnToken, tokens.Peek());
        }

        private IASTLeaf ParseContinue(ref TokenSource tokens)
        {
            Token token;
            tokens.Next(TokenType.Keyword, Keyword.CONTINUE, out token);
            ParseSemiColonForgiving(ref tokens);
            return NewLeaf<ContinueLeaf>(token, tokens.Peek());
        }

        private BreakLeaf ParseBreak(ref TokenSource tokens)
        {
            Token breakToken;
            tokens.Next(TokenType.Keyword, Keyword.BREAK, out breakToken);
            ParseSemiColonForgiving(ref tokens);
            return NewLeaf<BreakLeaf>(breakToken, tokens.Peek());
        }

        private IASTLeaf? ParseExpression(ref TokenSource tokens)
        {
            IASTLeaf? lhs = ParsePrimaryOrUnary(ref tokens);
            if (lhs == null) {
                return null;
            }
            return ParseOperator(ref tokens, lhs, -1);
        }

        private IASTLeaf? ParsePrimaryOrUnary(ref TokenSource tokens)
        {
            TokenSource primaryTokens = tokens.Fork();
            IASTLeaf? primaryLeaf = ParsePrimaryExpression(ref primaryTokens);
            if (primaryLeaf != null) {
                tokens.Consume(primaryTokens);
                return primaryLeaf;
            }

            // Expression is not primary, Must be a unary.
            Token token = tokens.Peek();
            
            if (token.Value == "(") {
                return ParseParenthesisExpression(ref tokens);    
            } 
            
            return ParseBasicUnaryOperator(ref tokens);
        }

        private IASTLeaf? ParseOperator(ref TokenSource tokens, IASTLeaf lhs, int precedence)
        {
            while (true) {
                Token tok = tokens.Peek();
                int rhsPrecedence = GetTokenPrecedence(tok);

                if (rhsPrecedence == -1) {
                    return lhs;
                }
                
                if (precedence > rhsPrecedence) {
                    return lhs;
                }
                
                if (tok.Value == "(") {
                    FunctionCallParseLeaf? functionCall = ParseFunctionCall(ref tokens, lhs);
                    if (functionCall == null) {
                        return null;
                    }
                    lhs = functionCall;
                    continue;
                }
                
                // Eat the operator
                tokens.Consume();

                IASTLeaf? rhs = ParsePrimaryOrUnary(ref tokens);
                if (rhs == null) {
                    return null;
                }
                
                Token nextToken = tokens.Peek();
                int nextPrecedence = GetTokenPrecedence(nextToken);

                if (nextPrecedence > rhsPrecedence) {
                    rhs = ParseOperator(ref tokens, rhs, rhsPrecedence + 1);
                    if (rhs == null) {
                        return null;
                    }
                }

                lhs = NewLeaf(new BinaryOperationExpressionLeaf(lhs, rhs, tok), lhs, tokens.Peek());
            }
        }

        private const int TOKEN_PRECEDENCE_UNARY = 7;
        
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
        
        private FunctionCallParseLeaf? ParseFunctionCall(ref TokenSource tokens, IASTLeaf lhs)
        {
            // Eat left paren
            if (!tokens.Next(TokenType.Syntax, "(", out _)) {
                return null;
            }
            
            List<IASTLeaf> arguments = new List<IASTLeaf>();
            
            bool expectingNextArgument = true;
            bool closingParensAllowed = true;

            while (true) {
                Token token = tokens.Peek();
                
                if (token.Type == TokenType.Syntax && token.Value == ")") {
                    if (!closingParensAllowed) {
                        tokens.AddError("Unexpected closing parenthesis", token);
                    }
                    tokens.Consume();
                    break;
                }
                
                if (!expectingNextArgument) {
                    tokens.AddError("Expecting closing parenthesis", token);
                    return null;
                }

                IASTLeaf? argument = ParseExpression(ref tokens);
                if (argument == null) {
                    return null;
                }
                arguments.Add(argument);

                token = tokens.Peek();
                if (token.Type == TokenType.Syntax && token.Value == ",") {
                    tokens.Consume();
                    closingParensAllowed = false;

                } else {
                    closingParensAllowed = true;
                    expectingNextArgument = false;
                }
            }

            return NewLeaf(new FunctionCallParseLeaf(lhs, arguments), lhs, tokens.Peek());
        }

        private IASTLeaf? ParsePrimaryExpression(ref TokenSource tokens)
        {
            Token token = tokens.Peek();
            
            if (token.Type == TokenType.Identifier) {
                return ParseIdentifierExpression(ref tokens);
            }

            if (token.Type == TokenType.Number) {
                return ParseNumericLiteralExpression(ref tokens);
            }

            if (token.Type == TokenType.String) {
                return ParseStringLiteralExpression(ref tokens);
            }
            
            tokens.AddError("Unexpected token while parsing primary expression", token);
            return null;
        }

        private IASTLeaf? ParseParenthesisExpression(ref TokenSource tokens)
        {
            if (!tokens.Next(TokenType.Syntax, "(", out _)) {
                return null;
            }

            IASTLeaf? expression = ParseExpression(ref tokens);
            if (expression == null) {
                return null;
            }

            if (!tokens.Next(TokenType.Syntax, ")", out _)) {
                return null;
            }

            return expression;
        }

        private IASTLeaf? ParseBasicUnaryOperator(ref TokenSource tokens)
        {
            Token op = tokens.Next();
            IASTLeaf? rhs = ParsePrimaryExpression(ref tokens);
            if (rhs == null) {
                return null;
            }
            rhs = ParseOperator(ref tokens, rhs, TOKEN_PRECEDENCE_UNARY);
            if (rhs == null) {
                return null;
            }
            return NewLeaf(new UnaryOperationLeaf(op, rhs), op, tokens.Peek());
        }

        private IdentifierParseLeaf? ParseIdentifierExpression(ref TokenSource tokens)
        {
            Token token;
            if (!tokens.Next(TokenType.Identifier, out token)) {
                return null;
            }

            return NewLeaf(new IdentifierParseLeaf(token.Value), token, tokens.Peek());
        }
        
        private NumericLiteralLeaf ParseNumericLiteralExpression(ref TokenSource tokens)
        {
            Token token;
            tokens.Next(TokenType.Number, out token);

            bool parseSuccess;
            int value;
            
            if (token.Value.StartsWith("0x")) {
                parseSuccess = int.TryParse(token.Value.Substring(2), NumberStyles.AllowHexSpecifier, null, out value);
            } else {
                parseSuccess = int.TryParse(token.Value, out value);
            }

            if (!parseSuccess) {
                tokens.AddError("Invalid numeric literal", token);
            }
            
            return NewLeaf(new NumericLiteralLeaf(value), token, tokens.Peek());
        }
        
        private StringLiteralLeaf? ParseStringLiteralExpression(ref TokenSource tokens)
        {
            Token token;
            if (!tokens.Next(TokenType.String, out token)) {
                return null;
            }
            return NewLeaf(new StringLiteralLeaf(token.Value), token, tokens.Peek());
        }

        private static readonly string[] PointerTokens = { "&", "^", "*", "?"};
        
        private TypeSpecifierLeaf? ParseTypeSpecifier(ref TokenSource tokens)
        {
            Token typenameToken;
            if (!tokens.Next(TokenType.Identifier, out typenameToken)) {
                return null;
            }
            
            Token next = tokens.Peek();

            PointerType pointerType = PointerType.NotAPointer;

            if (next.Type == TokenType.Syntax) {
                if (PointerTokens.Contains(next.Value)) {
                    switch (next.Value) {
                        case "&":
                            pointerType = PointerType.Borrowed;
                            break;
                        case "^":
                            pointerType = PointerType.Owned;
                            break;
                        case "*":
                            pointerType = PointerType.Shared;
                            break;
                        case "?":
                            pointerType = PointerType.Weak;
                            break;
                    }
                    tokens.Consume();
                }
            }

            return NewLeaf(new TypeSpecifierLeaf(typenameToken.Value, pointerType), typenameToken, tokens.Peek());
        }

        private void ParseSemiColonForgiving(ref TokenSource tokens)
        {
            Token t = tokens.Peek();
            if (!(t.Type == TokenType.Syntax && t.Value == ";")) {
                tokens.AddError("Expecting semicolon", t);
                return;
            }
            tokens.Consume();
        }

        private T NewLeaf<T>(Token startToken, Token endToken) where T : IASTLeaf, new()
        {
            T leaf = new T();
            return NewLeaf(leaf, startToken, endToken);
        }

        private T NewLeaf<T>(T leaf, Token startToken, Token endToken) where T : IASTLeaf
        {
            return NewLeaf(leaf, startToken.Location, endToken.DeriveEndLocation());
        }

        private T NewLeaf<T>(T leaf, FileLocation start, FileLocation end) where T : IASTLeaf
        {
            Symbol symbol = new Symbol {
                Leaf = leaf,
                SourceFile = _filePath,
                Start = start,
                End = end
            };

            _tokenMap[leaf] = symbol;
            return leaf;
        }

        private T NewLeaf<T>(T leaf, IASTLeaf startLeaf, Token endToken) where T : IASTLeaf
        {
            Symbol symbol;
            _tokenMap.TryGetValue(startLeaf, out symbol);
            return NewLeaf(leaf, symbol.Start, endToken.DeriveEndLocation());
        }
    }
}