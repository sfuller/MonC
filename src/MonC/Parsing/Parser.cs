using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MonC.Parsing;
using MonC.Parsing.ParseTree.Nodes;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.BinaryOperations;
using MonC.SyntaxTree.Nodes.Expressions.UnaryOperations;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.TypeSystem;


namespace MonC
{
    public class Parser
    {
        private readonly List<Token> _tokens = new List<Token>();
        private string? _filePath;

        private IDictionary<ISyntaxTreeNode, Symbol> _symbolMap = new Dictionary<ISyntaxTreeNode, Symbol>();

        public ParseModule Parse(string? filePath, IEnumerable<Token> tokens, IList<ParseError> errors)
        {
            _filePath = filePath;
            _tokens.Clear();
            _tokens.AddRange(tokens);

            TokenSource tokenSource = new TokenSource(_tokens, 0, errors);

            ParseModule outputModule = new ParseModule();
            _symbolMap = outputModule.SymbolMap;

            ParseModuleHopper hopper = new ParseModuleHopper(outputModule);

            while (tokenSource.Peek().Type != TokenType.None) {
                ParseTopLevelStatement(ref tokenSource)?.AcceptTopLevelVisitor(hopper);
            }

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

            public bool HasErrors => _errors?.Count > 0;

            public readonly Token Peek(int offset = 0)
            {
                int i = _offset + offset;
                if (i < 0 || i >= _tokens.Count) {
                    return new Token();
                }
                return _tokens[i];
            }

            private bool Peek(int offset, TokenType type, out Token token)
            {
                token = Peek(offset);
                if (token.Type != type) {
                    return false;
                }
                return true;
            }

            private bool Peek(int offset, TokenType type, string value, out Token token)
            {
                token = Peek(offset);
                if (token.Type != type || token.Value != value) {
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
                if (!result) {
                    AddError(new ParseError {
                        Message = $"Expecting token of type {type}, got {token.Type}",
                        Start = token.Location,
                        End = token.DeriveEndLocation()
                    });
                }
                Consume();
                return result;
            }

            public bool Next(TokenType type, string value, out Token token)
            {
                bool result = Peek(0, type, value, out token);
                if (!result) {
                    AddError(new ParseError {
                        Message =
                            $"Expecting token of type {type} with value {value}, got {token.Type} with value {token.Value}",
                        Start = token.Location,
                        End = token.DeriveEndLocation()
                    });
                }
                Consume();
                return result;
            }

            public bool TryNext(TokenType type, out Token token)
            {
                bool matches = Peek(0, type, out token);
                if (matches) {
                    Consume();
                }
                return matches;
            }

            public bool TryNext(TokenType type, string value, out Token token)
            {
                bool matches = Peek(0, type, value, out token);
                if (matches) {
                    Consume();
                }
                return matches;
            }

            private void AddError(ParseError error)
            {
                _errors ??= new List<ParseError>();
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
                if (other._errors != null) {
                    _errors ??= new List<ParseError>();
                    foreach (ParseError error in other._errors) {
                        _errors.Add(error);
                    }
                }
            }
        }

        private ITopLevelStatementNode? ParseTopLevelStatement(ref TokenSource tokens)
        {
            bool isExported = true;

            Token token = tokens.Peek();
            if (token.Type == TokenType.Keyword && token.Value == Keyword.STATIC) {
                isExported = false;
                tokens.Consume();
                token = tokens.Peek();
            }

            if (token.Type == TokenType.Keyword && token.Value == Keyword.ENUM) {
                return ParseEnum(ref tokens, isExported);
            }

            TokenSource structTokens = tokens.Fork();
            StructNode? structNode = ParseStruct(ref structTokens, isExported);
            if (structNode != null) {
                tokens.Consume(structTokens);
                return structNode;
            }

            return ParseFunction(ref tokens, isExported);
        }

        private EnumNode? ParseEnum(ref TokenSource tokens, bool isExported)
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

            List<EnumDeclarationNode> declarations = ParseCommaSeparatedNodes(ref tokens, ParseEnumDeclaration);
            tokens.TryNext(TokenType.Syntax, Syntax.CLOSING_BRACKET, out _);
            return NewNode(new EnumNode(nameToken.Value, declarations, isExported), startToken, tokens.Peek(-1));
        }

        private EnumDeclarationNode? ParseEnumDeclaration(ref TokenSource tokens)
        {
            if (!tokens.TryNext(TokenType.Identifier, out Token nameToken)) {
                return null;
            }
            return new EnumDeclarationNode(nameToken.Value);
        }

        private StructNode? ParseStruct(ref TokenSource tokens, bool isExported)
        {
            if (!tokens.Next(TokenType.Keyword, Keyword.STRUCT, out Token startToken)) {
                return null;
            }

            string name = "";
            if (tokens.TryNext(TokenType.Identifier, out Token nameToken)) {
                name = nameToken.Value;
            }

            List<IStructFunctionAssociationNode> functionAssociations;

            if (tokens.TryNext(TokenType.Syntax, Syntax.STRUCT_FUNCTION_ASSOCIATION_STARTER, out _)) {
                functionAssociations = ParseCommaSeparatedNodes(ref tokens, parseStructFunctionAssociation);
            } else {
                functionAssociations = new List<IStructFunctionAssociationNode>();
            }

            tokens.TryNext(TokenType.Syntax, Syntax.OPENING_BRACKET, out _);

            List<DeclarationNode> declarations = new List<DeclarationNode>();

            while (true) {
                TokenSource declarationTokens = tokens.Fork();
                DeclarationNode? declarationNode = ParseDeclaration(ref declarationTokens);
                if (declarationNode == null) {
                    break;
                }
                declarations.Add(declarationNode);
                Terminate(ref declarationTokens, Syntax.SEMICOLON);
                tokens.Consume(declarationTokens);
            }

            tokens.TryNext(TokenType.Syntax, Syntax.CLOSING_BRACKET, out _);

            return NewNode(
                    new StructNode(name, functionAssociations, declarations, isExported),
                    startToken,
                    tokens.Peek(-1));
        }

        private IStructFunctionAssociationNode? parseStructFunctionAssociation(ref TokenSource tokens)
        {
            if (!tokens.Next(TokenType.Identifier, out Token associationNameToken)) {
                return null;
            }

            if (!tokens.Next(TokenType.Syntax, Syntax.STRUCT_FUNCTION_ASSOCIATION_SEPARATOR, out _)) {
                return null;
            }

            tokens.TryNext(TokenType.Identifier, out Token functionNameToken);
            return new StructFunctionAssociationParseNode(associationNameToken.Value, functionNameToken.Value);
        }

        private FunctionDefinitionNode? ParseFunction(ref TokenSource tokens, bool isExported)
        {
            Token retunTypeStart = tokens.Peek();
            TypeSpecifierParseNode? returnType = ParseTypeSpecifier(ref tokens);
            if (returnType == null) {
                returnType = new TypeSpecifierParseNode("", PointerMode.NotAPointer);
            }

            Token name;
            if (!(
                tokens.Next(TokenType.Identifier, out name)
                && tokens.Next(TokenType.Syntax, Syntax.OPENING_PAREN, out _)
            )) {
                return null;
            }

            List<DeclarationNode> parameters = ParseCommaSeparatedNodes(ref tokens, ParseDeclaration);
            tokens.Next(TokenType.Syntax, Syntax.CLOSING_PAREN, out _);

            bool isDrop = tokens.TryNext(TokenType.Identifier, Syntax.FUNCTION_ATTRIBUTE_DROP, out _);

            BodyNode? body = ParseBody(ref tokens);
            if (body == null) {
                return null;
            }

            return NewNode(
                new FunctionDefinitionNode(name.Value, returnType, parameters, body, isExported, isDrop),
                retunTypeStart, tokens.Peek(-1));
        }

        private BodyNode? ParseBody(ref TokenSource tokens)
        {
            if (!tokens.Next(TokenType.Syntax, Syntax.OPENING_BRACKET, out Token bodyOpening)) {
                return null;
            }

            List<IStatementNode> statements = new List<IStatementNode>();

            while (true) {
                if (MatchOrUnexpectedEof(ref tokens, TokenType.Syntax, Syntax.CLOSING_BRACKET)) {
                    break;
                }

                IStatementNode? statement = ParseStatement(ref tokens);
                if (statement != null) {
                    statements.Add(statement);
                }
            }

            return NewNode(new BodyNode(statements), bodyOpening, tokens.Peek(-1));
        }

        private IStatementNode? ParseStatement(ref TokenSource tokens)
        {
            IStatementNode? TryNodeTypes(in TokenSource tokens, out TokenSource tokensToConsume)
            {
                tokensToConsume = tokens.Fork();
                IStatementNode? statement = ParseDeclaration(ref tokensToConsume);
                if (statement != null) {
                    Terminate(ref tokensToConsume, Syntax.SEMICOLON);
                    return statement;
                }

                tokensToConsume = tokens.Fork();
                statement = ParseFlow(ref tokensToConsume);
                if (statement != null) {
                    return statement;
                }

                tokensToConsume = tokens.Fork();
                IExpressionNode? expression = ParseExpression(ref tokensToConsume);
                if (expression != null) {
                    statement = new ExpressionStatementNode(expression);
                    Terminate(ref tokensToConsume, Syntax.SEMICOLON);
                    return statement;
                }

                return null;
            }

            IStatementNode? statement = TryNodeTypes(in tokens, out TokenSource tokensToConsume);
            tokens.Consume(tokensToConsume);
            return statement;
        }

        private DeclarationNode? ParseDeclaration(ref TokenSource tokens)
        {
            Token startToken = tokens.Peek();
            TypeSpecifierParseNode? typeSpecifier = ParseTypeSpecifier(ref tokens);
            Token nameToken;
            if (typeSpecifier == null || !tokens.Next(TokenType.Identifier, out nameToken)) {
                return null;
            }

            IExpressionNode? assignment = null;

            Token nextToken = tokens.Peek();

            if (nextToken.Type == TokenType.Syntax && nextToken.Value == Syntax.BINOP_ASSIGN) {
                tokens.Consume();
                assignment = ParseExpression(ref tokens);
            }

            if (assignment == null) {
                assignment = new VoidExpressionNode();
            }

            return NewNode(new DeclarationNode(typeSpecifier, nameToken.Value, assignment), startToken, tokens.Peek(-1));
        }

        private DeclarationNode CreatePlaceholderDeclaration(Token startToken, Token endToken)
        {
            return NewNode(
                new DeclarationNode(
                    new TypeSpecifierParseNode("void", PointerMode.NotAPointer),
                    "",
                    new VoidExpressionNode()
                ),
                startToken,
                endToken);
        }

        private IStatementNode? ParseFlow(ref TokenSource tokens)
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

        private IfElseNode? ParseIfElse(ref TokenSource tokens)
        {
            Token ifToken;
            if (!(tokens.Next(TokenType.Keyword, Keyword.IF, out ifToken) && tokens.Next(TokenType.Syntax, Syntax.OPENING_PAREN, out _))) {
                return null;
            }

            IExpressionNode condition = ParseRequiredExpressionWithTerminator(ref tokens, Syntax.CLOSING_PAREN);
            BodyNode ifBody = ParseRequiredBody(ref tokens);
            BodyNode? elseBody = null;
            Token nextToken = tokens.Peek();

            if (nextToken.Type == TokenType.Keyword && nextToken.Value == Keyword.ELSE) {
                tokens.Consume();
                elseBody = ParseRequiredBody(ref tokens);
            }

            return NewNode(new IfElseNode(condition, ifBody, elseBody ?? new BodyNode()), ifToken, tokens.Peek(-1));
        }

        private WhileNode? ParseWhile(ref TokenSource tokens)
        {
            Token whileToken;
            if (!(tokens.Next(TokenType.Keyword, Keyword.WHILE, out whileToken) && tokens.Next(TokenType.Syntax, Syntax.OPENING_PAREN, out _))) {
                return null;
            }

            IExpressionNode condition = ParseRequiredExpressionWithTerminator(ref tokens, Syntax.CLOSING_PAREN);
            BodyNode body = ParseRequiredBody(ref tokens);
            return NewNode(new WhileNode(condition, body), whileToken, tokens.Peek(-1));
        }

        private ForNode? ParseFor(ref TokenSource tokens)
        {
            Token forToken;
            if (!(tokens.Next(TokenType.Keyword, Keyword.FOR, out forToken) && tokens.Next(TokenType.Syntax, Syntax.OPENING_PAREN, out _))) {
                return null;
            }

            TokenSource declarationTokens = tokens.Fork();
            Token declarationStart = declarationTokens.Peek();
            DeclarationNode? declaration = ParseDeclaration(ref declarationTokens);
            Token declarationEnd = declarationTokens.Peek(-1);
            Terminate(ref declarationTokens, Syntax.SEMICOLON);
            tokens.Consume(declarationTokens);
            declaration ??= CreatePlaceholderDeclaration(declarationStart, declarationEnd);

            IExpressionNode condition = ParseRequiredExpressionWithTerminator(ref tokens, Syntax.SEMICOLON);
            IExpressionNode update = ParseRequiredExpressionWithTerminator(ref tokens, Syntax.CLOSING_PAREN);
            BodyNode body = ParseRequiredBody(ref tokens);
            return NewNode(new ForNode(declaration, condition, update, body), forToken, tokens.Peek(-1));
        }

        private ReturnNode ParseReturn(ref TokenSource tokens)
        {
            Token returnToken;
            tokens.Next(TokenType.Keyword, Keyword.RETURN, out returnToken);

            Token token = tokens.Peek();

            IExpressionNode? expression = null;

            TokenSource expressionTokens = tokens.Fork();

            if (!(token.Type == TokenType.Syntax && token.Value == Syntax.SEMICOLON)) {
                expression = ParseExpression(ref expressionTokens);
            }
            expression ??= new VoidExpressionNode();

            Terminate(ref expressionTokens, Syntax.SEMICOLON);
            tokens.Consume(expressionTokens);
            return NewNode(new ReturnNode(expression), returnToken, tokens.Peek(-1));
        }

        private ContinueNode ParseContinue(ref TokenSource tokens)
        {
            Token token;
            tokens.Next(TokenType.Keyword, Keyword.CONTINUE, out token);
            ParseSemiColonForgiving(ref tokens);
            return NewNode<ContinueNode>(token, tokens.Peek(-1));
        }

        private BreakNode ParseBreak(ref TokenSource tokens)
        {
            Token breakToken;
            tokens.Next(TokenType.Keyword, Keyword.BREAK, out breakToken);
            ParseSemiColonForgiving(ref tokens);
            return NewNode<BreakNode>(breakToken, tokens.Peek(-1));
        }

        private IExpressionNode ParseRequiredExpressionWithTerminator(ref TokenSource tokens, string syntaxValue)
        {
            TokenSource expressionTokens = tokens.Fork();
            Token startToken = expressionTokens.Peek();
            IExpressionNode? expression = ParseExpression(ref expressionTokens);
            Token endToken = expressionTokens.Peek(-1);
            Terminate(ref expressionTokens, syntaxValue);
            tokens.Consume(expressionTokens);
            return expression ?? NewNode(new VoidExpressionNode(), startToken, endToken);
        }

        private BodyNode ParseRequiredBody(ref TokenSource tokens)
        {
            Token bodyStart = tokens.Peek();
            BodyNode? body = ParseBody(ref tokens);
            Token bodyEnd = tokens.Peek(-1);
            return body ?? NewNode(new BodyNode(), bodyStart, bodyEnd);
        }

        private IExpressionNode? ParseExpression(ref TokenSource tokens, int previousPrecedence = -1)
        {
            IExpressionNode? lhs = ParseNonBinaryExpression(ref tokens);
            if (lhs == null) {
                return null;
            }

            // Process binary operations.
            while (true) {
                Token opToken = tokens.Peek();
                int precedence = GetTokenPrecedence(opToken);
                if (precedence > previousPrecedence) {
                    tokens.Consume(); // Eat operator
                    lhs = CreateBinOpNodeByOperator(ref tokens, opToken, lhs, precedence);
                    if (lhs == null) {
                        return null;
                    }
                } else {
                    break;
                }
            }

            return lhs;
        }

        private IExpressionNode? ParseNonBinaryExpression(ref TokenSource tokens)
        {
            TokenSource primaryTokens = tokens.Fork();
            IExpressionNode? primaryNode = ParsePrimaryExpression(ref primaryTokens);
            if (primaryNode != null) {
                tokens.Consume(primaryTokens);
                TokenSource primaryOperatorTokens = tokens.Fork();
                IExpressionNode? primaryOperator = ParsePrimaryOperator(ref primaryOperatorTokens, primaryNode);
                if (primaryOperator != null) {
                    tokens.Consume(primaryOperatorTokens);
                    return primaryOperator;
                }
                return primaryNode;
            }

            TokenSource unaryTokens = tokens.Fork();
            IUnaryOperationNode? unaryNode = ParseUnaryOperation(ref unaryTokens);
            if (unaryNode != null) {
                tokens.Consume(unaryTokens);
                return unaryNode;
            }

            Token token = tokens.Peek();
            if (token.Value == Syntax.OPENING_PAREN) {
                return ParseParenthesisExpression(ref tokens);
            }

            Token unexpectedToken = tokens.Next();
            tokens.AddError("Unexpected token", unexpectedToken);
            return null;
        }

        private IExpressionNode? CreateBinOpNodeByOperator(
                ref TokenSource tokens,
                Token token,
                IExpressionNode lhs,
                int precedence)
        {
            IExpressionNode? rawNode;
            ISyntaxTreeNode rhs;

            if (token.Value == Syntax.BINOP_ACCESS) {
                DeclarationIdentifierParseNode? identifier = ParseDeclarationIdentifier(ref tokens);
                if (identifier == null) {
                    return null;
                }
                rhs = identifier;
                rawNode = new AccessParseNode(lhs, identifier);
            } else {
                IExpressionNode? rhsExpression = ParseExpression(ref tokens, precedence);
                if (rhsExpression == null) {
                    return null;
                }
                rhs = rhsExpression;

                rawNode = token.Value switch {
                    Syntax.BINOP_LESS_THAN => new CompareLtBinOpNode(lhs, rhsExpression),
                    Syntax.BINOP_GREATER_THAN => new CompareGtBinOpNode(lhs, rhsExpression),
                    Syntax.BINOP_LESS_THAN_OR_EQUAL_TO => new CompareLteBinOpNode(lhs, rhsExpression),
                    Syntax.BINOP_GREATER_THAN_OR_EQUAL_TO => new CompareGteBinOpNode(lhs, rhsExpression),
                    Syntax.BINOP_EQUALS => new CompareEqualityBinOpNode(lhs, rhsExpression),
                    Syntax.BINOP_NOT_EQUALS => new CompareInequalityBinOpNode(lhs, rhsExpression),
                    Syntax.BINOP_LOGICAL_AND => new LogicalAndBinOpNode(lhs, rhsExpression),
                    Syntax.BINOP_LOGICAL_OR => new LogicalOrBinOpNode(lhs, rhsExpression),
                    Syntax.BINOP_ASSIGN => new AssignmentParseNode(lhs, rhsExpression),
                    Syntax.BINOP_ADD => new AddBinOpNode(lhs, rhsExpression),
                    Syntax.BINOP_SUBTRACT => new SubtractBinOpNode(lhs, rhsExpression),
                    Syntax.BINOP_MULTIPLY => new MultiplyBinOpNode(lhs, rhsExpression),
                    Syntax.BINOP_DIVIDE => new DivideBinOpNode(lhs, rhsExpression),
                    Syntax.BINOP_MODULO => new ModuloBinOpNode(lhs, rhsExpression),
                    _ => null
                };
            }

            if (rawNode == null) {
                tokens.AddError($"Unrecognized binary operator {token.Value}", token);
                return null;
            }
            return NewNode(rawNode, lhs, rhs);
        }

        private const int TOKEN_PRECEDENCE_UNARY = 7;

        private int GetTokenPrecedence(Token token)
        {
            if (token.Type != TokenType.Syntax) {
                return -1;
            }

            switch (token.Value) {
                case Syntax.BINOP_ACCESS:
                    return 8;
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

        private FunctionCallParseNode? ParseFunctionCall(ref TokenSource tokens, IExpressionNode lhs)
        {
            // Eat left paren
            if (!tokens.Next(TokenType.Syntax, Syntax.OPENING_PAREN, out _)) {
                return null;
            }

            List<IExpressionNode> arguments = new List<IExpressionNode>();

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

                IExpressionNode? argument = ParseExpression(ref tokens);
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

            return NewNode(new FunctionCallParseNode(lhs, arguments), lhs, tokens.Peek(-1));
        }

        private IExpressionNode? ParsePrimaryExpression(ref TokenSource tokens)
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

        private IExpressionNode? ParsePrimaryOperator(ref TokenSource tokens, IExpressionNode primary)
        {
            TokenSource functionCallTokens = tokens.Fork();
            IExpressionNode? functionCall = ParseFunctionCall(ref functionCallTokens, primary);
            if (functionCall != null) {
                tokens.Consume(functionCallTokens);
                return functionCall;
            }

            // Note: More primary operators will go here in the future if FunctionCall isn't parsed.
            return null;
        }

        private IExpressionNode? ParseParenthesisExpression(ref TokenSource tokens)
        {
            if (!tokens.Next(TokenType.Syntax, Syntax.OPENING_PAREN, out _)) {
                return null;
            }

            IExpressionNode? expression = ParseExpression(ref tokens);
            if (expression == null) {
                return null;
            }

            if (!tokens.Next(TokenType.Syntax, Syntax.CLOSING_PAREN, out _)) {
                return null;
            }

            return expression;
        }

        private IUnaryOperationNode? ParseUnaryOperation(ref TokenSource tokens)
        {
            TokenSource castTokens = tokens.Fork();
            CastUnaryOpNode? castNode = ParseCast(ref castTokens);
            if (castNode != null) {
                tokens.Consume(castTokens);
                return castNode;
            }

            Token op = tokens.Next();
            return CreateUnaryOpNodeByOperator(ref tokens, op);
        }

        private IUnaryOperationNode? CreateUnaryOpNodeByOperator(ref TokenSource tokens, Token token)
        {
            IUnaryOperationNode? MakeUnary(ref TokenSource tokens, Func<IExpressionNode, IUnaryOperationNode> factory) {
                IExpressionNode? rhs = ParseExpression(ref tokens, TOKEN_PRECEDENCE_UNARY);
                if (rhs == null) {
                    return null;
                }
                return factory(rhs);
            }

            Token startToken = tokens.Peek();

            IUnaryOperationNode? rawNode = token.Value switch {
                Syntax.UNOP_NEGATE => MakeUnary(ref tokens, rhs => new NegateUnaryOpNode(rhs)),
                Syntax.UNOP_LOGICAL_NOT => MakeUnary(ref tokens, rhs => new LogicalNotUnaryOpNode(rhs)),
                _ => null
            };

            if (rawNode == null) {
                tokens.AddError($"Unrecognized binary operator {token.Value}", token);
                return null;
            }
            return NewNode(rawNode, startToken, tokens.Peek(-1));
        }

        private CastUnaryOpNode? ParseCast(ref TokenSource tokens)
        {
            if (!tokens.Next(TokenType.Syntax, Syntax.OPENING_PAREN, out Token startToken)) {
                return null;
            }

            TypeSpecifierParseNode? typeSpecifier = ParseTypeSpecifier(ref tokens);
            if (typeSpecifier == null) {
                return null;
            }

            tokens.Next(TokenType.Syntax, Syntax.CLOSING_PAREN, out Token endToken);

            IExpressionNode? rhs = ParseExpression(ref tokens, TOKEN_PRECEDENCE_UNARY);
            if (rhs == null) {
                return null;
            }

            return NewNode(new CastUnaryOpNode(typeSpecifier, rhs), startToken, endToken);
        }

        private IdentifierParseNode? ParseIdentifierExpression(ref TokenSource tokens)
        {
            Token token;
            if (!tokens.Next(TokenType.Identifier, out token)) {
                return null;
            }

            return NewNode(new IdentifierParseNode(token.Value), token, tokens.Peek(-1));
        }

        private DeclarationIdentifierParseNode? ParseDeclarationIdentifier(ref TokenSource tokens)
        {
            if (tokens.Next(TokenType.Identifier, out Token token)) {
                return NewNode(new DeclarationIdentifierParseNode(token.Value), token, token);
            }
            return null;
        }

        private NumericLiteralNode ParseNumericLiteralExpression(ref TokenSource tokens)
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

            return NewNode(new NumericLiteralNode(value), token, tokens.Peek(-1));
        }

        private StringLiteralNode? ParseStringLiteralExpression(ref TokenSource tokens)
        {
            Token token;
            if (!tokens.Next(TokenType.String, out token)) {
                return null;
            }
            return NewNode(new StringLiteralNode(token.Value), token, tokens.Peek(-1));
        }

        private static readonly string[] PointerTokens = {
            Syntax.POINTER_BORROWED,
            Syntax.POINTER_OWNED,
            Syntax.POINTER_SHARED,
            Syntax.POINTER_WEAK
        };

        private TypeSpecifierParseNode? ParseTypeSpecifier(ref TokenSource tokens)
        {
            Token typenameToken;
            if (!tokens.Next(TokenType.Identifier, out typenameToken)) {
                return null;
            }

            Token next = tokens.Peek();

            PointerMode mode = PointerMode.NotAPointer;

            if (next.Type == TokenType.Syntax) {
                if (PointerTokens.Contains(next.Value)) {
                    mode = next.Value switch {
                        Syntax.POINTER_BORROWED => PointerMode.Borrowed,
                        Syntax.POINTER_OWNED => PointerMode.Owned,
                        Syntax.POINTER_SHARED => PointerMode.Shared,
                        Syntax.POINTER_WEAK => PointerMode.Weak,
                        _ => mode
                    };
                    tokens.Consume();
                }
            }

            return new TypeSpecifierParseNode(typenameToken.Value, mode);
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
        /// If the given TokenSource has errors, consume until a token with the given syntax value is found.
        /// Otherwise, expect a token with the given syntax value, and add an error if it doesn't exist.
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="syntaxValue"></param>
        private void Terminate(ref TokenSource tokens, string syntaxValue)
        {
            if (!tokens.HasErrors) {
                if (!tokens.TryNext(TokenType.Syntax, syntaxValue, out Token token)) {
                    tokens.AddError("Expecting " + syntaxValue, token);
                }
                return;
            }

            while (true) {
                Token nextToken = tokens.Peek();
                if (nextToken.Type == TokenType.None) {
                    // EOF, stop looping. All future tokens will be TokenType.None.
                    return;
                }
                tokens.Consume();
                if (nextToken.Type == TokenType.Syntax && nextToken.Value == syntaxValue) {
                    return;
                }
            }
        }

        private bool MatchOrUnexpectedEof(ref TokenSource tokens, TokenType type, string value)
        {
            Token token = tokens.Peek();
            if (token.Type == TokenType.None) {
                tokens.AddError("Unexpected EOF", token);
                return true;
            }
            if (token.Type == type && token.Value == value) {
                tokens.Consume();
                return true;
            }
            return false;
        }

        private delegate T? ParseNodeAction<out T>(ref TokenSource tokens) where T : class;

        private List<T> ParseCommaSeparatedNodes<T>(ref TokenSource tokens, ParseNodeAction<T> nodeAction)
                where T : class
        {
            List<T> nodes = new List<T>();

            while (true) {
                TokenSource nodeTokens = tokens.Fork();
                T? node = nodeAction(ref nodeTokens);
                if (node == null) {
                    break;
                }
                nodes.Add(node);
                tokens.Consume(nodeTokens);
                if (!tokens.TryNext(TokenType.Syntax, Syntax.COMMA, out _)) {
                    break;
                }
            }

            return nodes;
        }

        /// <summary>
        /// Create a new node instance, generate a symbol based on start and end tokens, and associate the symbol with
        /// the new node instance.
        /// </summary>
        /// <param name="startToken">The first token associated with the node.</param>
        /// <param name="endToken">The last token associated with the node.</param>
        /// <typeparam name="T">The type of node to create.</typeparam>
        /// <returns>The new node instance.</returns>
        private T NewNode<T>(Token startToken, Token endToken) where T : ISyntaxTreeNode, new()
        {
            T node = new T();
            return NewNode(node, startToken, endToken);
        }

        /// <summary>
        /// Generates a symbol based on start and end tokens, and associates the symbol with the given node instance.
        /// </summary>
        /// <param name="node">The node to associate with the new symbol.</param>
        /// <param name="startToken">The first token associated with the node.</param>
        /// <param name="endToken">The last token associated with the node.</param>
        /// <typeparam name="T">The node instance that was passed as <see cref="node"/>.</typeparam>
        /// <returns>The node instance that was passed as <see cref="node"/>.</returns>
        private T NewNode<T>(T node, Token startToken, Token endToken) where T : ISyntaxTreeNode
        {
            return NewNode(node, startToken.Location, endToken.DeriveEndLocation());
        }

        /// <summary>
        /// Generates a symbol based on the start and end file locations, and associates the symbol with the given node
        /// instance.
        /// </summary>
        /// <param name="node">The node to associate with the new symbol.</param>
        /// <param name="start">The location in the file where the text associated with the given node starts.</param>
        /// <param name="end">The location in the file where the text associated with the given node ends.</param>
        /// <returns>The node instance that was passed as <see cref="node"/>.</returns>
        private T NewNode<T>(T node, FileLocation start, FileLocation end) where T : ISyntaxTreeNode
        {
            Symbol symbol = new Symbol {
                Node = node,
                SourceFile = _filePath,
                Start = start,
                End = end
            };

            _symbolMap[node] = symbol;
            return node;
        }

        /// <summary>
        /// Generates a symbol based on the starting file location of <see cref="startNode"/> and the end token.
        /// The symbol is associated wit the given node instance.
        /// </summary>
        /// <param name="node">The node to associate with the new symbol.</param>
        /// <param name="startNode">
        /// The node to get the starting file location. This node must be associated with a symbol.
        /// </param>
        /// <param name="endToken">The last token associated with the given node.</param>
        /// <returns>The node instance that was passed as <see cref="node"/>.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// No symbol is associated with <see cref="startNode"/>
        /// </exception>
        private T NewNode<T>(T node, ISyntaxTreeNode startNode, Token endToken) where T : ISyntaxTreeNode
        {
            if (!_symbolMap.TryGetValue(startNode, out Symbol symbol)) {
                throw new InvalidOperationException($"No symbol associated with {nameof(startNode)}");
            }
            return NewNode(node, symbol.Start, endToken.DeriveEndLocation());
        }

        /// <summary>
        /// Generates a symbol based on the starting file location of <see cref="startNode"/> and the ending file
        /// location of <see cref="endNode"/>.
        /// The symbol is associated wit the given node instance.
        /// </summary>
        /// <param name="node">The node to associate with the new symbol.</param>
        /// <param name="startNode">
        /// The node to get the starting file location from. This node must be associated with a symbol.
        /// </param>
        /// <param name="endNode">
        /// The node to get the end file location from. This node must be associated with a symbol.
        /// </param>
        /// <returns>The node instance that was passed as <see cref="node"/>.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// No symbol is associated with either <see cref="startNode"/> or <see cref="endNode"/>.
        /// </exception>
        private T NewNode<T>(T node, ISyntaxTreeNode startNode, ISyntaxTreeNode endNode) where T : ISyntaxTreeNode
        {
            if (!_symbolMap.TryGetValue(startNode, out Symbol startSymbol)) {
                throw new InvalidOperationException($"No symbol associated with {nameof(startNode)}");
            }
            if (!_symbolMap.TryGetValue(endNode, out Symbol endSymbol)) {
                throw new InvalidOperationException($"No symbol associated with {nameof(endNode)}");
            }
            return NewNode(node, startSymbol.Start, endSymbol.End);
        }

    }
}
