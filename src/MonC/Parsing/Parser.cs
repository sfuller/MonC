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
using MonC.SyntaxTree.Leaves.Expressions.UnaryOperations;
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
                if (i < 0 || i >= _tokens.Count) {
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

            return NewLeaf(new EnumLeaf(nameToken.Value, enumerations, isExported), startToken, tokens.Peek(-1));
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

            BodyLeaf? body = ParseBody(ref tokens);
            if (body == null) {
                return null;
            }

            return NewLeaf(
                new FunctionDefinitionLeaf(name.Value, returnType, parameters, body, isExported),
                retunTypeStart, tokens.Peek(-1));
        }

        private BodyLeaf? ParseBody(ref TokenSource tokens)
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

            return new BodyLeaf(statements);
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

            if (nextToken.Type == TokenType.Syntax && nextToken.Value == Syntax.BINOP_ASSIGN) {
                tokens.Consume();
                assignment = ParseExpression(ref tokens);
            }

            if (assignment == null) {
                assignment = new VoidExpression();
            }

            return NewLeaf(new DeclarationLeaf(typeSpecifier, nameToken.Value, assignment), startToken, tokens.Peek(-1));
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

            return NewLeaf(new IfElseLeaf(condition, ifBody, elseBody ?? new BodyLeaf()), ifToken, tokens.Peek(-1));
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

            BodyLeaf? body = ParseBody(ref tokens);
            if (body == null) {
                return null;
            }

            return NewLeaf(new WhileLeaf(condition, body), whileToken, tokens.Peek(-1));
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

            BodyLeaf? body = ParseBody(ref tokens);
            if (body == null) {
                return null;
            }

            return NewLeaf(new ForLeaf(declaration, condition, update, body), forToken, tokens.Peek(-1));
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

            return NewLeaf(new ReturnLeaf(expression), returnToken, tokens.Peek(-1));
        }

        private ContinueLeaf ParseContinue(ref TokenSource tokens)
        {
            Token token;
            tokens.Next(TokenType.Keyword, Keyword.CONTINUE, out token);
            ParseSemiColonForgiving(ref tokens);
            return NewLeaf<ContinueLeaf>(token, tokens.Peek(-1));
        }

        private BreakLeaf ParseBreak(ref TokenSource tokens)
        {
            Token breakToken;
            tokens.Next(TokenType.Keyword, Keyword.BREAK, out breakToken);
            ParseSemiColonForgiving(ref tokens);
            return NewLeaf<BreakLeaf>(breakToken, tokens.Peek(-1));
        }

        private IExpressionLeaf? ParseExpression(ref TokenSource tokens, int previousPrecedence = -1)
        {
            IExpressionLeaf? lhs = ParseNonBinaryExpression(ref tokens);
            if (lhs == null) {
                return null;
            }

            // Process binary operations.
            while (true) {
                Token opToken = tokens.Peek();
                int precedence = GetTokenPrecedence(opToken);
                if (precedence > previousPrecedence) {
                    tokens.Next(); // Eat operator
                    IExpressionLeaf? rhs = ParseExpression(ref tokens, precedence);
                    if (rhs == null) {
                        return null;
                    }
                    lhs = CreateBinOpLeafByOperator(ref tokens, opToken, lhs, rhs);
                    if (lhs == null) {
                        return null;
                    }
                } else {
                    break;
                }
            }

            return lhs;
        }


        private IExpressionLeaf? ParseNonBinaryExpression(ref TokenSource tokens)
        {
            TokenSource primaryTokens = tokens.Fork();
            IExpressionLeaf? primaryLeaf = ParsePrimaryExpression(ref primaryTokens);
            if (primaryLeaf != null) {
                tokens.Consume(primaryTokens);
                TokenSource primaryOperatorTokens = tokens.Fork();
                IExpressionLeaf? primaryOperator = ParsePrimaryOperator(ref primaryOperatorTokens, primaryLeaf);
                if (primaryOperator != null) {
                    tokens.Consume(primaryOperatorTokens);
                    return primaryOperator;
                }
                return primaryLeaf;
            }

            TokenSource unaryTokens = tokens.Fork();
            IUnaryOperationLeaf? unaryLeaf = ParseUnaryOperation(ref unaryTokens);
            if (unaryLeaf != null) {
                tokens.Consume(unaryTokens);
                return unaryLeaf;
            }

            Token token = tokens.Peek();
            if (token.Value == Syntax.OPENING_PAREN) {
                return ParseParenthesisExpression(ref tokens);
            }

            return ParseUnaryOperation(ref tokens);
        }

        private IBinaryOperationLeaf? CreateBinOpLeafByOperator(ref TokenSource tokens, Token token, IExpressionLeaf lhs, IExpressionLeaf rhs)
        {
            IBinaryOperationLeaf? rawLeaf = token.Value switch {
                Syntax.BINOP_LESS_THAN => new CompareLTBinOpLeaf(lhs, rhs),
                Syntax.BINOP_GREATER_THAN => new CompareGTBinOpLeaf(lhs, rhs),
                Syntax.BINOP_LESS_THAN_OR_EQUAL_TO => new CompareLTEBinOpLeaf(lhs, rhs),
                Syntax.BINOP_GREATER_THAN_OR_EQUAL_TO => new CompareGTEBinOpLeaf(lhs, rhs),
                Syntax.BINOP_EQUALS => new CompareEqualityBinOpLeaf(lhs, rhs),
                Syntax.BINOP_NOT_EQUALS => new CompareInequalityBinOpLeaf(lhs, rhs),
                Syntax.BINOP_LOGICAL_AND => new LogicalAndBinOpLeaf(lhs, rhs),
                Syntax.BINOP_LOGICAL_OR => new LogicalOrBinOpLeaf(lhs, rhs),
                Syntax.BINOP_ASSIGN => new AssignmentParseLeaf(lhs, rhs),
                Syntax.BINOP_ADD => new AddBinOpLeaf(lhs, rhs),
                Syntax.BINOP_SUBTRACT => new SubtractBinOpLeaf(lhs, rhs),
                Syntax.BINOP_MULTIPLY => new MultiplyBinOpLeaf(lhs, rhs),
                Syntax.BINOP_DIVIDE => new DivideBinOpLeaf(lhs, rhs),
                Syntax.BINOP_MODULO => new ModuloBinOpLeaf(lhs, rhs),
                _ => null
            };

            if (rawLeaf == null) {
                tokens.AddError($"Unrecognized binary operator {token.Value}", token);
                return null;
            }
            return NewLeaf(rawLeaf, lhs, rhs);
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
                case Syntax.BINOP_MULTIPLY:
                case Syntax.BINOP_DIVIDE:
                case Syntax.BINOP_MODULO:
                    return 6;
                case Syntax.BINOP_ADD:
                case Syntax.BINOP_SUBTRACT:
                    return 5;
                case Syntax.BINOP_LESS_THAN:
                case Syntax.BINOP_GREATER_THAN:
                case Syntax.BINOP_GREATER_THAN_OR_EQUAL_TO:
                case Syntax.BINOP_LESS_THAN_OR_EQUAL_TO:
                    return 4;
                case Syntax.BINOP_EQUALS:
                case Syntax.BINOP_NOT_EQUALS:
                    return 3;
                case Syntax.BINOP_LOGICAL_AND:
                    return 2;
                case Syntax.BINOP_LOGICAL_OR:
                    return 1;
                case Syntax.BINOP_ASSIGN:
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

            return NewLeaf(new FunctionCallParseLeaf(lhs, arguments), lhs, tokens.Peek(-1));
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

        private IExpressionLeaf? ParsePrimaryOperator(ref TokenSource tokens, IExpressionLeaf primary)
        {
            TokenSource functionCallTokens = tokens.Fork();
            IExpressionLeaf? functionCall = ParseFunctionCall(ref functionCallTokens, primary);
            if (functionCall != null) {
                tokens.Consume(functionCallTokens);
                return functionCall;
            }

            // Note: More primary operators will go here in the future if FunctionCall isn't parsed.
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

        private IUnaryOperationLeaf? ParseUnaryOperation(ref TokenSource tokens)
        {
            Token op = tokens.Next();
            IExpressionLeaf? rhs = ParseExpression(ref tokens, TOKEN_PRECEDENCE_UNARY);
            if (rhs == null) {
                return null;
            }
            return CreateUnaryOpLeafByOperator(ref tokens, op, rhs);
        }

        private IUnaryOperationLeaf? CreateUnaryOpLeafByOperator(ref TokenSource tokens, Token token, IExpressionLeaf rhs)
        {
            IUnaryOperationLeaf? rawLeaf = token.Value switch {
                Syntax.UNOP_NEGATE => new NegateUnaryOpLeaf(rhs),
                Syntax.UNOP_LOGICAL_NOT => new LogicalNotUnaryOpLeaf(rhs),
                _ => null
            };

            if (rawLeaf == null) {
                tokens.AddError($"Unrecognized binary operator {token.Value}", token);
                return null;
            }
            return NewLeaf(rawLeaf, token.Location, GetSymbolForLeaf(rhs).End);
        }

        private IdentifierParseLeaf? ParseIdentifierExpression(ref TokenSource tokens)
        {
            Token token;
            if (!tokens.Next(TokenType.Identifier, out token)) {
                return null;
            }

            return NewLeaf(new IdentifierParseLeaf(token.Value), token, tokens.Peek(-1));
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

            return NewLeaf(new NumericLiteralLeaf(value), token, tokens.Peek(-1));
        }

        private StringLiteralLeaf? ParseStringLiteralExpression(ref TokenSource tokens)
        {
            Token token;
            if (!tokens.Next(TokenType.String, out token)) {
                return null;
            }
            return NewLeaf(new StringLiteralLeaf(token.Value), token, tokens.Peek(-1));
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

        /// <summary>
        /// Create a new leaf instance, generate a symbol based on start and end tokens, and associate the symbol with
        /// the new leaf instance.
        /// </summary>
        /// <param name="startToken">The first token associated with the leaf.</param>
        /// <param name="endToken">The last token associated with the leaf.</param>
        /// <typeparam name="T">The type of leaf to create.</typeparam>
        /// <returns>The new leaf instance.</returns>
        private T NewLeaf<T>(Token startToken, Token endToken) where T : ISyntaxTreeLeaf, new()
        {
            T leaf = new T();
            return NewLeaf(leaf, startToken, endToken);
        }

        /// <summary>
        /// Generates a symbol based on start and end tokens, and associates the symbol with the given leaf instance.
        /// </summary>
        /// <param name="leaf">The leaf to associate with the new symbol.</param>
        /// <param name="startToken">The first token associated with the leaf.</param>
        /// <param name="endToken">The last token associated with the leaf.</param>
        /// <typeparam name="T">The leaf instance that was passed as <see cref="leaf"/>.</typeparam>
        /// <returns></returns>
        private T NewLeaf<T>(T leaf, Token startToken, Token endToken) where T : ISyntaxTreeLeaf
        {
            return NewLeaf(leaf, startToken.Location, endToken.DeriveEndLocation());
        }

        /// <summary>
        /// Generates a symbol based on the start and end file locations, and associates the symbol with the given leaf
        /// instance.
        /// </summary>
        /// <param name="leaf">The leaf to associate with the new symbol.</param>
        /// <param name="start">The location in the file where the text associated with the given leaf starts.</param>
        /// <param name="end">The location in the file where the text associated with the given leaf ends.</param>
        /// <returns>The leaf instance that was passed as <see cref="leaf"/>.</returns>
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

        /// <summary>
        /// Generates a symbol based on the starting file location of <see cref="startLeaf"/> and the end token.
        /// The symbol is associated wit the given leaf instance.
        /// </summary>
        /// <param name="leaf">The leaf to associate with the new symbol.</param>
        /// <param name="startLeaf">
        /// The leaf to get the starting file location. This leaf must be associated with a symbol.
        /// </param>
        /// <param name="endToken">The last token associated with the given leaf.</param>
        /// <returns>The leaf instance that was passed as <see cref="leaf"/>.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// No symbol is associated with <see cref="startLeaf"/>
        /// </exception>
        private T NewLeaf<T>(T leaf, ISyntaxTreeLeaf startLeaf, Token endToken) where T : ISyntaxTreeLeaf
        {
            if (!_tokenMap.TryGetValue(startLeaf, out Symbol symbol)) {
                throw new InvalidOperationException($"No symbol associated with {nameof(startLeaf)}");
            }
            return NewLeaf(leaf, symbol.Start, endToken.DeriveEndLocation());
        }

        /// <summary>
        /// Generates a symbol based on the starting file location of <see cref="startLeaf"/> and the ending file
        /// location of <see cref="endLeaf"/>.
        /// The symbol is associated wit the given leaf instance.
        /// </summary>
        /// <param name="leaf">The leaf to associate with the new symbol.</param>
        /// <param name="startLeaf">
        /// The leaf to get the starting file location from. This leaf must be associated with a symbol.
        /// </param>
        /// <param name="endLeaf">
        /// The leaf to get the end file location from. This leaf must be associated with a symbol.
        /// </param>
        /// <returns>The leaf instance that was passed as <see cref="leaf"/>.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// No symbol is associated with either <see cref="startLeaf"/> or <see cref="endLeaf"/>.
        /// </exception>
        private T NewLeaf<T>(T leaf, ISyntaxTreeLeaf startLeaf, ISyntaxTreeLeaf endLeaf) where T : ISyntaxTreeLeaf
        {
            if (!_tokenMap.TryGetValue(startLeaf, out Symbol startSymbol)) {
                throw new InvalidOperationException($"No symbol associated with {nameof(startLeaf)}");
            }
            if (!_tokenMap.TryGetValue(endLeaf, out Symbol endSymbol)) {
                throw new InvalidOperationException($"No symbol associated with {nameof(endLeaf)}");
            }
            return NewLeaf(leaf, startSymbol.Start, endSymbol.End);
        }

        private Symbol GetSymbolForLeaf(ISyntaxTreeLeaf leaf)
        {
            if (!_tokenMap.TryGetValue(leaf, out Symbol symbol)) {
                throw new InvalidOperationException($"No symbol associated with {nameof(leaf)}");
            }
            return symbol;
        }
    }
}
