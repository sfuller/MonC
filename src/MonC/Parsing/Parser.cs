using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MonC.Parsing;
using MonC.Parsing.ParseTreeLeaves;
using MonC.Parsing.Semantics;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Leaves;
using MonC.SyntaxTree.Leaves.Expressions;
using MonC.SyntaxTree.Leaves.Expressions.BinaryOperations;
using MonC.SyntaxTree.Leaves.Statements;


namespace MonC
{
    public class Parser
    {
        private readonly List<Token> _tokens = new List<Token>();
        private string? _filePath;

        private IDictionary<ISyntaxTreeLeaf, Symbol> _tokenMap = new Dictionary<ISyntaxTreeLeaf, Symbol>();

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

            SemanticAnalyzer analyzer = new SemanticAnalyzer(errors, _tokenMap);
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
            Token nameToken;

            if (!(
                tokens.Next(TokenType.Keyword, Keyword.ENUM, out _)
                && tokens.Next(TokenType.Identifier, out nameToken)
                && tokens.Next(TokenType.Syntax, Syntax.OPENING_BRACKET, out _)
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

                if (next.Type == TokenType.Syntax && next.Value == Syntax.CLOSING_BRACKET) {
                    if (!endIsAllowed) {
                        tokens.AddError("Expecting next enumeration", next);
                    }
                    tokens.Next();
                    break;
                }

                if (!nextEnumerationIsAllowed) {
                    tokens.AddError($"Expecting {Syntax.CLOSING_BRACKET}", next);
                    break;
                }

                Token name;
                if (!tokens.Next(TokenType.Identifier, out name)) {
                    break;
                }

                enumerations.Add(new KeyValuePair<string, int>(name.Value, enumerations.Count));

                next = tokens.Peek();
                if (next.Type == TokenType.Syntax && next.Value == Syntax.COMMA) {
                    endIsAllowed = false;
                    tokens.Next();
                } else {
                    endIsAllowed = true;
                    nextEnumerationIsAllowed = false;
                }
            }

            return NewLeaf(new EnumLeaf(nameToken.Value, enumerations, isExported), startToken, tokens.Peek());
        }

        private FunctionDefinitionLeaf? ParseFunction(ref TokenSource tokens, bool isExported)
        {
            var parameters = new List<DeclarationLeaf>();

            Token retunTypeStart = tokens.Peek();
            TypeSpecifier? returnType = ParseTypeSpecifier(ref tokens);
            if (returnType == null) {
                returnType = new TypeSpecifier("", PointerType.NotAPointer);
            }

            Token name;
            if (!(
                tokens.Next(TokenType.Identifier, out name)
                && tokens.Next(TokenType.Syntax, Syntax.OPENING_PAREN, out _)
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
                    if (comma.Type == TokenType.Syntax && comma.Value == Syntax.COMMA) {
                        tokens.Consume();
                        expectingNext = true;
                        expectingComma = false;
                        expectingEnd = false;
                        continue;
                    }
                }

                if (expectingEnd) {
                    Token rightParen = tokens.Peek();
                    if (rightParen.Type == TokenType.Syntax && rightParen.Value == Syntax.CLOSING_PAREN) {
                        tokens.Consume();
                        break;
                    }
                }

                tokens.AddError("Unexpected token", tokens.Peek());
                return null;
            }

            Body? body = ParseBody(ref tokens);
            if (body == null) {
                return null;
            }

            return NewLeaf(
                new FunctionDefinitionLeaf(name.Value, returnType, parameters, body, isExported),
                retunTypeStart, tokens.Peek());
        }

        private Body? ParseBody(ref TokenSource tokens)
        {
            if (!tokens.Next(TokenType.Syntax, Syntax.OPENING_BRACKET, out _)) {
                return null;
            }

            List<IStatementLeaf> statements = new List<IStatementLeaf>();

            while (true) {
                Token next = tokens.Peek();

                if (next.Type == TokenType.Syntax && next.Value == Syntax.CLOSING_BRACKET) {
                    tokens.Consume();
                    break;
                }

                IStatementLeaf? statement = ParseStatement(ref tokens);
                if (statement == null) {
                    return null;
                }
                statements.Add(statement);
            }

            return new Body(statements);
        }

        private IStatementLeaf? ParseStatement(ref TokenSource tokens)
        {
            IStatementLeaf? TryLeafTypes(in TokenSource tokens, out TokenSource tokensToConsume)
            {
                tokensToConsume = tokens.Fork();
                IStatementLeaf? statement = ParseDeclaration(ref tokensToConsume);
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
                IExpressionLeaf expression = ParseExpression(ref tokensToConsume) ?? new VoidExpression();
                statement = new ExpressionStatementLeaf(expression);
                ParseSemiColonForgiving(ref tokensToConsume);
                return statement;
            }

            IStatementLeaf? statement = TryLeafTypes(in tokens, out TokenSource tokensToConsume);
            tokens.Consume(tokensToConsume);
            return statement;
        }

        private DeclarationLeaf? ParseDeclaration(ref TokenSource tokens)
        {
            Token startToken = tokens.Peek();
            TypeSpecifier? typeSpecifier = ParseTypeSpecifier(ref tokens);
            Token nameToken;
            if (typeSpecifier == null || !tokens.Next(TokenType.Identifier, out nameToken)) {
                return null;
            }

            IExpressionLeaf? assignment = null;

            Token nextToken = tokens.Peek();

            if (nextToken.Type == TokenType.Syntax && nextToken.Value == Syntax.ASSIGN) {
                tokens.Consume();
                assignment = ParseExpression(ref tokens);
            }

            if (assignment == null) {
                assignment = new VoidExpression();
            }

            return NewLeaf(new DeclarationLeaf(typeSpecifier, nameToken.Value, assignment), startToken, nameToken);
        }

        private IStatementLeaf? ParseFlow(ref TokenSource tokens)
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
            if (!(tokens.Next(TokenType.Keyword, Keyword.IF, out ifToken) && tokens.Next(TokenType.Syntax, Syntax.OPENING_PAREN, out _))) {
                return null;
            }

            IExpressionLeaf? condition = ParseExpression(ref tokens);
            if (condition == null) {
                return null;
            }

            if (!tokens.Next(TokenType.Syntax, Syntax.CLOSING_PAREN, out _)) {
                return null;
            }

            Body? ifBody = ParseBody(ref tokens);
            if (ifBody == null) {
                return null;
            }

            Body? elseBody = null;
            Token nextToken = tokens.Peek();

            if (nextToken.Type == TokenType.Keyword && nextToken.Value == Keyword.ELSE) {
                tokens.Consume();
                elseBody = ParseBody(ref tokens);
            }

            return NewLeaf(new IfElseLeaf(condition, ifBody, elseBody ?? new Body()), ifToken, tokens.Peek());
        }

        private WhileLeaf? ParseWhile(ref TokenSource tokens)
        {
            Token whileToken;
            if (!(tokens.Next(TokenType.Keyword, Keyword.WHILE, out whileToken) && tokens.Next(TokenType.Syntax, Syntax.OPENING_PAREN, out _))) {
                return null;
            }

            IExpressionLeaf? condition = ParseExpression(ref tokens);
            if (condition == null) {
                return null;
            }

            if (!tokens.Next(TokenType.Syntax, Syntax.CLOSING_PAREN, out _)) {
                return null;
            }

            Body? body = ParseBody(ref tokens);
            if (body == null) {
                return null;
            }

            return NewLeaf(new WhileLeaf(condition, body), whileToken, tokens.Peek());
        }

        private ForLeaf? ParseFor(ref TokenSource tokens)
        {
            Token forToken;
            if (!(tokens.Next(TokenType.Keyword, Keyword.FOR, out forToken) && tokens.Next(TokenType.Syntax, Syntax.OPENING_PAREN, out _))) {
                return null;
            }

            DeclarationLeaf? declaration = ParseDeclaration(ref tokens);
            if (declaration == null) {
                return null;
            }

            ParseSemiColonForgiving(ref tokens);

            IExpressionLeaf? condition = ParseExpression(ref tokens);
            if (condition == null) {
                return null;
            }

            ParseSemiColonForgiving(ref tokens);

            IExpressionLeaf? update = ParseExpression(ref tokens);
            if (update == null) {
                return null;
            }

            if (!tokens.Next(TokenType.Syntax, Syntax.CLOSING_PAREN, out _)) {
                return null;
            }

            Body? body = ParseBody(ref tokens);
            if (body == null) {
                return null;
            }

            return NewLeaf(new ForLeaf(declaration, condition, update, body), forToken, tokens.Peek());
        }

        private ReturnLeaf ParseReturn(ref TokenSource tokens)
        {
            Token returnToken;
            tokens.Next(TokenType.Keyword, Keyword.RETURN, out returnToken);

            Token token = tokens.Peek();

            IExpressionLeaf? expression = null;

            if (!(token.Type == TokenType.Syntax && token.Value == Syntax.SEMICOLON)) {
                expression = ParseExpression(ref tokens);
            }

            if (expression == null) {
                expression = new VoidExpression();
            }

            ParseSemiColonForgiving(ref tokens);

            return NewLeaf(new ReturnLeaf(expression), returnToken, tokens.Peek());
        }

        private ContinueLeaf ParseContinue(ref TokenSource tokens)
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

        private IExpressionLeaf? ParseExpression(ref TokenSource tokens)
        {
            IExpressionLeaf? lhs = ParsePrimaryOrUnary(ref tokens);
            if (lhs == null) {
                return null;
            }
            return ParseOperator(ref tokens, lhs, -1);
        }

        private IExpressionLeaf? ParsePrimaryOrUnary(ref TokenSource tokens)
        {
            TokenSource primaryTokens = tokens.Fork();
            IExpressionLeaf? primaryLeaf = ParsePrimaryExpression(ref primaryTokens);
            if (primaryLeaf != null) {
                tokens.Consume(primaryTokens);
                return primaryLeaf;
            }

            // Expression is not primary, Must be a unary.
            Token token = tokens.Peek();

            if (token.Value == Syntax.OPENING_PAREN) {
                return ParseParenthesisExpression(ref tokens);
            }

            return ParseBasicUnaryOperator(ref tokens);
        }

        private IExpressionLeaf? ParseOperator(ref TokenSource tokens, IExpressionLeaf lhs, int precedence)
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

                if (tok.Value == Syntax.OPENING_PAREN) {
                    FunctionCallParseLeaf? functionCall = ParseFunctionCall(ref tokens, lhs);
                    if (functionCall == null) {
                        return null;
                    }
                    lhs = functionCall;
                    continue;
                }

                // Eat the operator
                tokens.Consume();

                IExpressionLeaf? rhs = ParsePrimaryOrUnary(ref tokens);
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

                lhs = CreateBinOpLeafByOperator(ref tokens, tok, lhs, rhs);
            }
        }

        private IBinaryOperationLeaf CreateBinOpLeafByOperator(ref TokenSource tokens, Token token, IExpressionLeaf lhs, IExpressionLeaf rhs)
        {
            IBinaryOperationLeaf? InstantiateRawLeaf()
            {
                return token.Value switch {
                    Syntax.LESS_THAN => new CompareLTBinOpLeaf(lhs, rhs),
                    Syntax.GREATER_THAN => new CompareGTBinOpLeaf(lhs, rhs),
                    Syntax.LESS_THAN_OR_EQUAL_TO => new CompareLTEBinOpLeaf(lhs, rhs),
                    Syntax.GREATER_THAN_OR_EQUAL_TO => new CompareGTEBinOpLeaf(lhs, rhs),
                    Syntax.EQUALS => new CompareEqualityBinOpLeaf(lhs, rhs),
                    Syntax.NOT_EQUALS => new CompareInequalityBinOpLeaf(lhs, rhs),
                    Syntax.LOGICAL_AND => new LogicalAndBinOpLeaf(lhs, rhs),
                    Syntax.LOGICAL_OR => new LogicalOrBinOpLeaf(lhs, rhs),
                    Syntax.ASSIGN => new AssignmentParseLeaf(lhs, rhs),
                    Syntax.ADD => new AddBinOpLeaf(lhs, rhs),
                    Syntax.SUBTRACT => new SubtractBinOpLeaf(lhs, rhs),
                    Syntax.MULTIPLY => new MultiplyBinOpLeaf(lhs, rhs),
                    Syntax.DIVIDE => new DivideBinOpLeaf(lhs, rhs),
                    Syntax.MODULO => new ModuloBinOpLeaf(lhs, rhs),
                    _ => null
                };
            }

            IBinaryOperationLeaf? rawLeaf = InstantiateRawLeaf();
            if (rawLeaf == null) {
                tokens.AddError($"Unrecognized binary operator {token.Value}", token);
                rawLeaf = new CompareEqualityBinOpLeaf(lhs, rhs); // Return any type of bin op.
            }
            return NewLeaf(rawLeaf, lhs, tokens.Peek());
        }

        private const int TOKEN_PRECEDENCE_UNARY = 7;

        private int GetTokenPrecedence(Token token)
        {
            if (token.Type != TokenType.Syntax) {
                return -1;
            }

            switch (token.Value) {
                case Syntax.OPENING_PAREN:
                    return 7;
                case Syntax.MULTIPLY:
                case Syntax.DIVIDE:
                case Syntax.MODULO:
                    return 6;
                case Syntax.ADD:
                case Syntax.SUBTRACT:
                    return 5;
                case Syntax.LESS_THAN:
                case Syntax.GREATER_THAN:
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
                case Syntax.ASSIGN:
                    return 0;
                default:
                    return -1;
            }
        }

        private FunctionCallParseLeaf? ParseFunctionCall(ref TokenSource tokens, IExpressionLeaf lhs)
        {
            // Eat left paren
            if (!tokens.Next(TokenType.Syntax, Syntax.OPENING_PAREN, out _)) {
                return null;
            }

            List<IExpressionLeaf> arguments = new List<IExpressionLeaf>();

            bool expectingNextArgument = true;
            bool closingParensAllowed = true;

            while (true) {
                Token token = tokens.Peek();

                if (token.Type == TokenType.Syntax && token.Value == Syntax.CLOSING_PAREN) {
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

                IExpressionLeaf? argument = ParseExpression(ref tokens);
                if (argument == null) {
                    return null;
                }
                arguments.Add(argument);

                token = tokens.Peek();
                if (token.Type == TokenType.Syntax && token.Value == Syntax.COMMA) {
                    tokens.Consume();
                    closingParensAllowed = false;

                } else {
                    closingParensAllowed = true;
                    expectingNextArgument = false;
                }
            }

            return NewLeaf(new FunctionCallParseLeaf(lhs, arguments), lhs, tokens.Peek());
        }

        private IExpressionLeaf? ParsePrimaryExpression(ref TokenSource tokens)
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

        private IExpressionLeaf? ParseParenthesisExpression(ref TokenSource tokens)
        {
            if (!tokens.Next(TokenType.Syntax, Syntax.OPENING_PAREN, out _)) {
                return null;
            }

            IExpressionLeaf? expression = ParseExpression(ref tokens);
            if (expression == null) {
                return null;
            }

            if (!tokens.Next(TokenType.Syntax, Syntax.CLOSING_PAREN, out _)) {
                return null;
            }

            return expression;
        }

        private IExpressionLeaf? ParseBasicUnaryOperator(ref TokenSource tokens)
        {
            Token op = tokens.Next();
            IExpressionLeaf? rhs = ParsePrimaryExpression(ref tokens);
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

            if (token.Value.StartsWith(Syntax.HEX_NUMERIC_PREFIX)) {
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

        private static readonly string[] PointerTokens = {
            Syntax.POINTER_BORROWED,
            Syntax.POINTER_OWNED,
            Syntax.POINTER_SHARED,
            Syntax.POINTER_WEAK
        };

        private TypeSpecifier? ParseTypeSpecifier(ref TokenSource tokens)
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
                        case Syntax.POINTER_BORROWED:
                            pointerType = PointerType.Borrowed;
                            break;
                        case Syntax.POINTER_OWNED:
                            pointerType = PointerType.Owned;
                            break;
                        case Syntax.POINTER_SHARED:
                            pointerType = PointerType.Shared;
                            break;
                        case Syntax.POINTER_WEAK:
                            pointerType = PointerType.Weak;
                            break;
                    }
                    tokens.Consume();
                }
            }

            return new TypeSpecifier(typenameToken.Value, pointerType);
        }

        private void ParseSemiColonForgiving(ref TokenSource tokens)
        {
            Token t = tokens.Peek();
            if (!(t.Type == TokenType.Syntax && t.Value == Syntax.SEMICOLON)) {
                tokens.AddError("Expecting semicolon", t);
                return;
            }
            tokens.Consume();
        }

        private T NewLeaf<T>(Token startToken, Token endToken) where T : ISyntaxTreeLeaf, new()
        {
            T leaf = new T();
            return NewLeaf(leaf, startToken, endToken);
        }

        private T NewLeaf<T>(T leaf, Token startToken, Token endToken) where T : ISyntaxTreeLeaf
        {
            return NewLeaf(leaf, startToken.Location, endToken.DeriveEndLocation());
        }

        private T NewLeaf<T>(T leaf, FileLocation start, FileLocation end) where T : ISyntaxTreeLeaf
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

        private T NewLeaf<T>(T leaf, ISyntaxTreeLeaf startLeaf, Token endToken) where T : ISyntaxTreeLeaf
        {
            Symbol symbol;
            _tokenMap.TryGetValue(startLeaf, out symbol);
            return NewLeaf(leaf, symbol.Start, endToken.DeriveEndLocation());
        }
    }
}
