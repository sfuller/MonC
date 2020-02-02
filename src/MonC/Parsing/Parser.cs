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
        private string? _filePath;

        private IList<ParseError> _errors = new List<ParseError>();

        private IDictionary<IASTLeaf, Symbol> _tokenMap = new Dictionary<IASTLeaf, Symbol>();
        
        public ParseModule Parse(string? filePath, IEnumerable<Token> tokens, ParseModule headerModule, IList<ParseError> errors)
        {
            _filePath = filePath;
            _tokens.Clear();
            _tokens.AddRange(tokens);

            _errors = errors;

            ParseModule outputModule = new ParseModule();
            _tokenMap = outputModule.TokenMap;
            
            while (Peek().Type != TokenType.None) {
                ParseTopLevelStatement(outputModule.Functions, outputModule.Enums);
            }
            
            SemanticAnalyzer analyzer = new SemanticAnalyzer(errors,_tokenMap);
            analyzer.Analyze(headerModule, outputModule);

            return outputModule;
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
                    Start = token.Location,
                    End = token.DeriveEndLocation()
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
                    Start = token.Location,
                    End = token.DeriveEndLocation()
                });
                return false;
            }

            return true;
        }

        private void AddError(string message, Token token)
        {
            _errors.Add(new ParseError {
                Message = message,
                Start = token.Location,
                End = token.DeriveEndLocation()
            });
        }

        private void ParseTopLevelStatement(IList<FunctionDefinitionLeaf> functions, IList<EnumLeaf> enums)
        {
            bool isExported = true;

            Token token = Peek();
            if (token.Type == TokenType.Keyword && token.Value == Keyword.STATIC) {
                isExported = false;
                Next();
                token = Peek();
            }

            if (token.Type == TokenType.Keyword && token.Value == Keyword.ENUM) {
                EnumLeaf? enumLeaf = ParseEnum(isExported);
                if (enumLeaf != null) {
                    enums.Add(enumLeaf);
                }
                return;
            }

            FunctionDefinitionLeaf? def = ParseFunction(isExported);
            if (def != null) {
                functions.Add(def);
            }
        }

        private EnumLeaf? ParseEnum(bool isExported)
        {
            Token startToken = Peek();
            
            if (!(
                Next(TokenType.Keyword, Keyword.ENUM, out _)
                && Next(TokenType.Syntax, "{", out _)
            )) {
                return null;
            }

            List<KeyValuePair<string, int>> enumerations = new List<KeyValuePair<string, int>>();

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
                
                enumerations.Add(new KeyValuePair<string, int>(name.Value, enumerations.Count));

                next = Peek();
                if (next.Type == TokenType.Syntax && next.Value == ",") {
                    endIsAllowed = false;
                    Next();
                } else {
                    endIsAllowed = true;
                    nextEnumerationIsAllowed = false;
                }
            }

            return NewLeaf(new EnumLeaf(enumerations, isExported), startToken);
        }
        
        private FunctionDefinitionLeaf? ParseFunction(bool isExported)
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
                    var paramType = Peek();
                    var paramName = Peek(1);

                    if (paramType.Type == TokenType.Identifier && paramName.Type == TokenType.Identifier) {
                        DeclarationLeaf declLeaf = NewLeaf(
                                new DeclarationLeaf(paramType.Value, paramName.Value, null),
                                paramType,
                                paramName);
                        
                        parameters.Add(declLeaf);
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

            BodyLeaf? body = ParseBody();
            if (body == null) {
                return null;
            }

            return NewLeaf(
                new FunctionDefinitionLeaf(name.Value, returnType.Value, parameters, body, isExported),
                returnType);
        }

        private BodyLeaf? ParseBody()
        {
            Token bodyOpening;
            if (!Next(TokenType.Syntax, "{", out bodyOpening)) {
                return null;
            }
            
            List<IASTLeaf> statements = new List<IASTLeaf>();
            
            while (true) {
                Token next = Peek();
                
                if (next.Type == TokenType.Syntax && next.Value == "}") {
                    Consume();
                    break;
                }

                IASTLeaf? statement = ParseStatement();
                if (statement == null) {
                    return null;
                }
                statements.Add(statement);
            }
            
            return NewLeaf(new BodyLeaf(statements), bodyOpening);
        }

        private IASTLeaf? ParseStatement()
        {
            IASTLeaf? statement;
            
            if (CheckDeclaration()) {
                statement = ParseDeclaration();
                ParseSemiColonForgiving();
            }
            else if (CheckFlow()) {
                statement = ParseFlow();
            } 
            else {
                statement = ParseExpression();
                ParseSemiColonForgiving();
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
                || token.Value == Keyword.RETURN
                || token.Value == Keyword.CONTINUE
                || token.Value == Keyword.BREAK;
        }

        private DeclarationLeaf? ParseDeclaration()
        {
            Token typeToken, nameToken;
            if (!(Next(TokenType.Identifier, out typeToken) && Next(TokenType.Identifier, out nameToken))) {
                return null;
            }

            IASTLeaf? assignment = null;

            Token nextToken = Peek();
            
            if (nextToken.Type == TokenType.Syntax && nextToken.Value == "=") {
                Consume();
                assignment = ParseExpression();
            }

            return NewLeaf(new DeclarationLeaf(typeToken.Value, nameToken.Value, assignment), typeToken, nameToken);
        }

        private IASTLeaf? ParseFlow()
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
                case Keyword.RETURN:
                    return ParseReturn();
                case Keyword.CONTINUE:
                    return ParseContinue();
                case Keyword.BREAK:
                    return ParseBreak();
            }
            
            AddError("Unexpected token", token);
            return null;
        }

        private IfElseLeaf? ParseIfElse()
        {
            Token ifToken;
            if (!(Next(TokenType.Keyword, Keyword.IF, out ifToken) && Next(TokenType.Syntax, "(", out _))) {
                return null;
            }

            IASTLeaf? condition = ParseExpression();
            if (condition == null) {
                return null;
            }

            if (!Next(TokenType.Syntax, ")", out _)) {
                return null;
            }

            BodyLeaf? ifBody = ParseBody();
            if (ifBody == null) {
                return null;
            }

            BodyLeaf? elseBody = null;
            Token nextToken = Peek();

            if (nextToken.Type == TokenType.Keyword && nextToken.Value == Keyword.ELSE) {
                Consume();
                elseBody = ParseBody();
            }
            
            return NewLeaf(new IfElseLeaf(condition, ifBody, elseBody), ifToken);
        }

        private WhileLeaf? ParseWhile()
        {
            Token whileToken;
            if (!(Next(TokenType.Keyword, Keyword.WHILE, out whileToken) && Next(TokenType.Syntax, "(", out _))) {
                return null;
            }

            IASTLeaf? condition = ParseExpression();
            if (condition == null) {
                return null;
            }

            if (!Next(TokenType.Syntax, ")", out _)) {
                return null;
            }

            BodyLeaf? body = ParseBody();
            if (body == null) {
                return null;
            }

            return NewLeaf(new WhileLeaf(condition, body), whileToken);
        }

        private ForLeaf? ParseFor()
        {
            Token forToken;
            if (!(Next(TokenType.Keyword, Keyword.FOR, out forToken) && Next(TokenType.Syntax, "(", out _))) {
                return null;
            }

            DeclarationLeaf? declaration = ParseDeclaration();
            if (declaration == null) {
                return null;
            }
            
            ParseSemiColonForgiving();

            IASTLeaf? condition = ParseExpression();
            if (condition == null) {
                return null;
            }
            
            ParseSemiColonForgiving();

            IASTLeaf? update = ParseExpression();
            if (update == null) {
                return null;
            }

            if (!Next(TokenType.Syntax, ")", out _)) {
                return null;
            }

            BodyLeaf? body = ParseBody();
            if (body == null) {
                return null;
            }

            return NewLeaf(new ForLeaf(declaration, condition, update, body), forToken);
        }

        private ReturnLeaf? ParseReturn()
        {
            Token returnToken;
            Next(TokenType.Keyword, Keyword.RETURN, out returnToken);

            Token token = Peek();

            IASTLeaf? expression = null;
            
            if (!(token.Type == TokenType.Syntax && token.Value == ";")) {
                expression = ParseExpression();
            }
            
            ParseSemiColonForgiving();

            return NewLeaf(new ReturnLeaf {RHS = expression}, returnToken);
        }

        private IASTLeaf ParseContinue()
        {
            Token token;
            Next(TokenType.Keyword, Keyword.CONTINUE, out token);
            ParseSemiColonForgiving();
            return NewLeaf<ContinueLeaf>(token);
        }

        private BreakLeaf ParseBreak()
        {
            Token breakToken;
            Next(TokenType.Keyword, Keyword.BREAK, out breakToken);
            ParseSemiColonForgiving();
            return NewLeaf<BreakLeaf>(breakToken);
        }

        private IASTLeaf? ParseExpression()
        {
            IASTLeaf? lhs = ParsePrimaryOrUnary();
            if (lhs == null) {
                return null;
            }
            return ParseOperator(lhs, -1);
        }

        private IASTLeaf? ParsePrimaryOrUnary()
        {
            if (CheckPrimaryExpression()) {
                return ParsePrimaryExpression();
            }

            // Expression is not primary, Must be a unary.
            Token token = Peek();
            
            if (token.Value == "(") {
                return ParseParenthesisExpression();    
            } 
            
            return ParseBasicUnaryOperator();
        }

        private IASTLeaf? ParseOperator(IASTLeaf lhs, int precedence)
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
                    FunctionCallParseLeaf? functionCall = ParseFunctionCall(lhs);
                    if (functionCall == null) {
                        return null;
                    }
                    lhs = functionCall;
                    continue;
                }
                
                // Eat the operator
                Consume();

                IASTLeaf? rhs = ParsePrimaryOrUnary();
                if (rhs == null) {
                    return null;
                }
                
                Token nextToken = Peek();
                int nextPrecedence = GetTokenPrecedence(nextToken);

                if (nextPrecedence > rhsPrecedence) {
                    rhs = ParseOperator(rhs, rhsPrecedence + 1);
                    if (rhs == null) {
                        return null;
                    }
                }

                lhs = NewLeaf(new BinaryOperationExpressionLeaf(lhs, rhs, tok), lhs);
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
        
        private FunctionCallParseLeaf? ParseFunctionCall(IASTLeaf lhs)
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
                    return null;
                }

                IASTLeaf? argument = ParseExpression();
                if (argument == null) {
                    return null;
                }
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

            return NewLeaf(new FunctionCallParseLeaf(lhs, arguments), lhs);
        }

        private bool CheckPrimaryExpression()
        {
            Token token = Peek();
            
            // Primary expressions do not start with Syntax.
            // For example, unary operators. If a unary operator is encountered, another expression must be parsed.
            if (token.Type == TokenType.Syntax) {
                return false;
            }

            return true;
        }
        
        private IASTLeaf? ParsePrimaryExpression()
        {
            Token token = Peek();
            
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

        private IASTLeaf? ParseParenthesisExpression()
        {
            if (!Next(TokenType.Syntax, "(", out _)) {
                return null;
            }

            IASTLeaf? expression = ParseExpression();
            if (expression == null) {
                return null;
            }

            if (!Next(TokenType.Syntax, ")", out _)) {
                return null;
            }

            return expression;
        }

        private IASTLeaf? ParseBasicUnaryOperator()
        {
            Token op = Next();
            IASTLeaf? rhs = ParsePrimaryExpression();
            if (rhs == null) {
                return null;
            }
            rhs = ParseOperator(rhs, TOKEN_PRECEDENCE_UNARY);
            if (rhs == null) {
                return null;
            }
            return NewLeaf(new UnaryOperationLeaf(op, rhs), op);
        }

        private IdentifierParseLeaf? ParseIdentifierExpression()
        {
            Token token;
            if (!Next(TokenType.Identifier, out token)) {
                return null;
            }

            return NewLeaf(new IdentifierParseLeaf(token.Value), token);
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
            
            return NewLeaf(new NumericLiteralLeaf(value), token);
        }
        
        private StringLiteralLeaf? ParseStringLiteralExpression()
        {
            Token token;
            if (!Next(TokenType.String, out token)) {
                return null;
            }
            return NewLeaf(new StringLiteralLeaf(token.Value), token);
        }

        private bool ParseSemicolon()
        {
            Token token = Peek();
            if (token.Type != TokenType.Syntax || token.Value != ";") {
                AddError("Expecting semicolon", token);
                return false;
            }
            Consume();
            return true;
        }

        private void ParseSemiColonForgiving()
        {
            Token t = Peek();
            if (!(t.Type == TokenType.Syntax && t.Value == ";")) {
                AddError("Expecting semicolon", t);
                return;
            }
            Consume();
        }

        private T NewLeaf<T>(Token startToken, Token endToken) where T : IASTLeaf, new()
        {
            T leaf = new T();
            return NewLeaf(leaf, startToken, endToken);
        }

        private T NewLeaf<T>(Token startToken) where T : IASTLeaf, new()
        {
            T leaf = new T();
            return NewLeaf(leaf, startToken);
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
        
        private T NewLeaf<T>(T leaf, Token startToken) where T : IASTLeaf
        {
            return NewLeaf(leaf, startToken, Peek());
        }

        private T NewLeaf<T>(T leaf, IASTLeaf startLeaf) where T : IASTLeaf
        {
            Symbol symbol;
            _tokenMap.TryGetValue(startLeaf, out symbol);
            return NewLeaf(leaf, symbol.Start, Peek().DeriveEndLocation());
        }
    }
}