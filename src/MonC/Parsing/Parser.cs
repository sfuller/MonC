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

        private IDictionary<IASTLeaf, TokenRange> _tokenMap;
        
        public void Parse(IEnumerable<Token> tokens, Module module, IList<ParseError> errors, IList<FunctionDefinitionLeaf> functions)
        {
            _tokens.Clear();
            _tokens.AddRange(tokens);

            _errors = errors;
            _tokenMap = module.TokenMap;

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
                return NewLeaf<PlaceholderLeaf>(token);
            }

            FunctionDefinitionLeaf def;
            if (ParseFunction(isExported).Get(out def)) {
                functions.Add(def);
                return def;
            }
            return NewLeaf<PlaceholderLeaf>(token);
        }

        private Optional<EnumLeaf> ParseEnum(bool isExported)
        {
            Token startToken = Peek();
            
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

            return new Optional<EnumLeaf>(NewLeaf(new EnumLeaf(names, isExported), startToken));
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
                        DeclarationLeaf declLeaf = NewLeaf(new DeclarationLeaf(paramType.Value, paramName.Value, new Optional<IASTLeaf>()), paramType, paramName);
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
                return new Optional<FunctionDefinitionLeaf>();
            }

            BodyLeaf body;
            if (!ParseBody().Get(out body)) {
                return new Optional<FunctionDefinitionLeaf>();
            }
            
            return new Optional<FunctionDefinitionLeaf>(
                NewLeaf(new FunctionDefinitionLeaf(name.Value, returnType.Value, parameters, body, isExported), returnType)
            );
        }

        private Optional<BodyLeaf> ParseBody()
        {
            Token bodyOpening;
            if (!Next(TokenType.Syntax, "{", out bodyOpening)) {
                return new Optional<BodyLeaf>();
            }
            
            List<IASTLeaf> statements = new List<IASTLeaf>();
            
            while (true) {
                Token next = Peek();
                
                if (next.Type == TokenType.Syntax && next.Value == "}") {
                    Consume();
                    break;
                }

                IASTLeaf statement;
                if (!ParseStatement().Get(out statement)) {
                    return new Optional<BodyLeaf>();
                }
                statements.Add(statement);

                //next = Peek();

//                if (next.Type == TokenType.None) {
//                    AddError("Expected end of body", next);
//                    break;
//                }
            }
            
            return new Optional<BodyLeaf>(NewLeaf(new BodyLeaf(statements), bodyOpening));
        }

        private Optional<IASTLeaf> ParseStatement()
        {
            Optional<IASTLeaf> statement;
            
            if (CheckDeclaration()) {
                statement = ParseDeclaration().Abstract<IASTLeaf>();
                ParseSemiColonForgiving();
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

        private Optional<DeclarationLeaf> ParseDeclaration()
        {
            Token typeToken, nameToken;
            if (!(Next(TokenType.Identifier, out typeToken) && Next(TokenType.Identifier, out nameToken))) {
                return new Optional<DeclarationLeaf>();
            }

            Optional<IASTLeaf> assignment = new Optional<IASTLeaf>();

            Token nextToken = Peek();
            
            if (nextToken.Type == TokenType.Syntax && nextToken.Value == "=") {
                Consume();
                assignment = ParseExpression();
            }

            return new Optional<DeclarationLeaf>(
                NewLeaf(new DeclarationLeaf(typeToken.Value, nameToken.Value, assignment), typeToken, nameToken)
            );
        }

        private Optional<IASTLeaf> ParseFlow()
        {
            Token token = Peek();

            if (token.Type != TokenType.Keyword) {
                AddError("Expecting keyword", token);
                return new Optional<IASTLeaf>();
            }

            switch (token.Value) {
                case Keyword.IF:
                    return ParseIfElse().Abstract<IASTLeaf>();
                case Keyword.WHILE:
                    return ParseWhile().Abstract<IASTLeaf>();
                case Keyword.FOR:
                    return ParseFor().Abstract<IASTLeaf>();
                case Keyword.RETURN:
                    return new Optional<IASTLeaf>(ParseReturn());
            }
            
            AddError("Unexpected token", token);
            return new Optional<IASTLeaf>();
        }

        private Optional<IfElseLeaf> ParseIfElse()
        {
            Token ifToken;
            if (!(Next(TokenType.Keyword, Keyword.IF, out ifToken) && Next(TokenType.Syntax, "(", out _))) {
                return new Optional<IfElseLeaf>();
            }

            IASTLeaf condition;
            if (!ParseExpression().Get(out condition)) {
                return new Optional<IfElseLeaf>();
            }

            if (!Next(TokenType.Syntax, ")", out _)) {
                return new Optional<IfElseLeaf>();
            }

            BodyLeaf ifBody;
            if (!ParseBody().Get(out ifBody)) {
                return new Optional<IfElseLeaf>();
            }
            
            Optional<BodyLeaf> elseBody = new Optional<BodyLeaf>();
            Token nextToken = Peek();

            if (nextToken.Type == TokenType.Keyword && nextToken.Value == Keyword.ELSE) {
                Consume();
                elseBody = ParseBody();
            }
            
            return new Optional<IfElseLeaf>(NewLeaf(new IfElseLeaf(condition, ifBody, elseBody), ifToken));
        }

        private Optional<WhileLeaf> ParseWhile()
        {
            Token whileToken;
            if (!(Next(TokenType.Keyword, Keyword.WHILE, out whileToken) && Next(TokenType.Syntax, "(", out _))) {
                return new Optional<WhileLeaf>();
            }

            IASTLeaf condition;
            if (!ParseExpression().Get(out condition)) {
                return new Optional<WhileLeaf>();
            }

            if (!Next(TokenType.Syntax, ")", out _)) {
                return new Optional<WhileLeaf>();
            }

            BodyLeaf body;
            if (!ParseBody().Get(out body)) {
                return new Optional<WhileLeaf>();
            }

            return new Optional<WhileLeaf>(NewLeaf(new WhileLeaf(condition, body), whileToken));
        }

        private Optional<ForLeaf> ParseFor()
        {
            Token forToken;
            if (!(Next(TokenType.Keyword, Keyword.FOR, out forToken) && Next(TokenType.Syntax, "(", out _))) {
                return new Optional<ForLeaf>();
            }

            DeclarationLeaf declaration;
            if (!ParseDeclaration().Get(out declaration)) {
                return new Optional<ForLeaf>();
            }
            
            ParseSemiColonForgiving();
            
            IASTLeaf condition;
            if (!ParseExpression().Get(out condition)) {
                return new Optional<ForLeaf>();
            }
            
            ParseSemiColonForgiving();

            IASTLeaf update;
            if (!ParseExpression().Get(out update)) {
                return new Optional<ForLeaf>();
            }

            if (!Next(TokenType.Syntax, ")", out _)) {
                return new Optional<ForLeaf>();
            }

            BodyLeaf body;
            if (!ParseBody().Get(out body)) {
                return new Optional<ForLeaf>();
            }

            return new Optional<ForLeaf>(
                NewLeaf(new ForLeaf(declaration, condition, update, body), forToken)
            );
        }

        private ReturnLeaf ParseReturn()
        {
            Token returnToken;
            Next(TokenType.Keyword, Keyword.RETURN, out returnToken);

            Token token = Peek();

            Optional<IASTLeaf> expression;
            
            if (token.Type == TokenType.Syntax && token.Value == ";") {
                expression = new Optional<IASTLeaf>();
            } else {
                expression = ParseExpression();
            }
            
            ParseSemiColonForgiving();

            return NewLeaf(new ReturnLeaf {RHS = expression}, returnToken);
        }

        private Optional<IASTLeaf> ParseExpression()
        {
            IASTLeaf primary;
            if (!ParsePrimaryExpression().Get(out primary)) {
                return new Optional<IASTLeaf>();
            }
            return ParseOperator(primary, -1);
        }

        private Optional<IASTLeaf> ParseOperator(IASTLeaf lhs, int precedence)
        {
            while (true) {
                Token tok = Peek();
                int rhsPrecedence = GetTokenPrecedence(tok);

                if (rhsPrecedence == -1) {
                    return new Optional<IASTLeaf>(lhs);
                }
                
                if (precedence > rhsPrecedence) {
                    return new Optional<IASTLeaf>(lhs);
                }
                
                if (tok.Value == "(") {
                    FunctionCallParseLeaf functionCall;
                    if (!ParseFunctionCall(lhs).Get(out functionCall)) {
                        return new Optional<IASTLeaf>();
                    }
                    lhs = functionCall;
                    continue;
                }
                
                // Eat the operator
                Consume();

                IASTLeaf rhs; 
                if (!ParsePrimaryExpression().Get(out rhs)) {
                    return new Optional<IASTLeaf>();
                }
                
                Token nextToken = Peek();
                int nextPrecedence = GetTokenPrecedence(nextToken);

                if (nextPrecedence > rhsPrecedence) {
                    if (!ParseOperator(rhs, rhsPrecedence + 1).Get(out rhs)) {
                        return new Optional<IASTLeaf>();
                    }
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
        
        private Optional<FunctionCallParseLeaf> ParseFunctionCall(IASTLeaf lhs)
        {
            Token leftParen;
            
            // Eat left paren
            if (!Next(TokenType.Syntax, "(", out leftParen)) {
                return new Optional<FunctionCallParseLeaf>();
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
                    return new Optional<FunctionCallParseLeaf>();
                }

                IASTLeaf argument;
                if (!ParseExpression().Get(out argument)) {
                    return new Optional<FunctionCallParseLeaf>();
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
            
            return new Optional<FunctionCallParseLeaf>(
                NewLeaf(new FunctionCallParseLeaf(lhs, arguments, leftParen), leftParen)
            );
        }

        private Optional<IASTLeaf> ParsePrimaryExpression()
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
                return ParseIdentifierExpression().Abstract<IASTLeaf>();
            }

            if (token.Type == TokenType.Number) {
                return new Optional<IASTLeaf>(ParseNumericLiteralExpression());
            }

            if (token.Type == TokenType.String) {
                return ParseStringLiteralExpression().Abstract<IASTLeaf>();
            }
            
            AddError("Unexpected token while parsing primary expression", token);
            return new Optional<IASTLeaf>();
        }

        private Optional<IASTLeaf> ParseParenthesisExpression()
        {
            if (!Next(TokenType.Syntax, "(", out _)) {
                return new Optional<IASTLeaf>();
            }
            
            IASTLeaf expression;
            if (!ParseExpression().Get(out expression)) {
                return new Optional<IASTLeaf>();
            }

            if (!Next(TokenType.Syntax, ")", out _)) {
                return new Optional<IASTLeaf>();
            }
            
            return new Optional<IASTLeaf>(expression);
        }

        private Optional<IASTLeaf> ParseNegateOperator()
        {
            Token op = Next();
            IASTLeaf rhs;
            if (!ParseExpression().Get(out rhs)) {
                return new Optional<IASTLeaf>();
            }
            return new Optional<IASTLeaf>(NewLeaf(new UnaryOperationLeaf(op, rhs), op));
        }

        private Optional<IdentifierParseLeaf> ParseIdentifierExpression()
        {
            Token token;
            if (!Next(TokenType.Identifier, out token)) {
                return new Optional<IdentifierParseLeaf>();
            }
            
            return new Optional<IdentifierParseLeaf>(
                NewLeaf(new IdentifierParseLeaf(token.Value), token)
            );
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
        
        private Optional<StringLiteralLeaf> ParseStringLiteralExpression()
        {
            Token token;
            if (!Next(TokenType.String, out token)) {
                return new Optional<StringLiteralLeaf>();
            }
            return new Optional<StringLiteralLeaf>(NewLeaf(new StringLiteralLeaf(token.Value), token));
        }

        private bool ParseSemicolon()
        {
            return Next(TokenType.Syntax, ";", out _);
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
            _tokenMap[leaf] = new TokenRange {Start = startToken, End = endToken};
            return leaf;
        }
        
        private T NewLeaf<T>(T leaf, Token startToken) where T : IASTLeaf
        {
            return NewLeaf(leaf, startToken, Peek());
        }
        
    }
}