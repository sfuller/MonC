using System;
using System.Collections.Generic;
using System.Text;

namespace MonC
{
    public class Lexer
    {
        struct StringLexState
        {
            public bool ConsumedFirstQuote;
            public bool IsEscaping;
        }
        
        private string _sourceString = "";
        private int _sourceIndex;
        private uint _currentLine;
        private uint _currentColumn;
        
        private TokenType _currentTokenType;
        private readonly StringBuilder _valueBuffer = new StringBuilder();
        private StringLexState _stringState;

        public void Lex(string source, IList<Token> tokens)
        {
            _sourceString = source;
            _sourceIndex = 0;
            
            while (Lex(tokens)) { }
        }

        private bool Lex(IList<Token> tokens)
        {
            if (_currentTokenType == TokenType.None) {
                _currentTokenType = GetNextTokenType();
            }

            bool canContinue;
            
            switch (_currentTokenType) {
                default:
                    tokens.Add(MakeToken());
                    canContinue = false;
                    break;
                case TokenType.Identifier:
                    canContinue = ProcessIdentifier(tokens);
                    break;
                case TokenType.Number:
                    canContinue = ProcessNumber(tokens);
                    break;
                case TokenType.String:
                    canContinue = ProcessString(tokens);
                    break;
                case TokenType.Syntax:
                    canContinue = ProcessSyntax(tokens);
                    break;
            }

            if (canContinue) {
                _currentTokenType = TokenType.None;
            }
            return canContinue;
        }

        private TokenType GetNextTokenType(bool ignoreSpace = true)
        {
            int next;
            char nextChar;
            
            if (ignoreSpace) {
                // Skip non-token characters
                while ((next = Peek()) != -1) {
                    nextChar = (char) next;
                    if (Char.IsWhiteSpace(nextChar)) {
                        Consume();
                    } else {
                        break;
                    }
                }    
            }
            else {
                next = Peek();
            }

            if (next == -1 || next == '\0') {
                return TokenType.None;
            }

            nextChar = (char)next;
            
            if (IsIdentifierOpener(nextChar)) {
                return TokenType.Identifier;
            }
            if (Char.IsNumber(nextChar)) {
                return TokenType.Number;
            }
            if (nextChar == '"') {
                _stringState = new StringLexState(); // TODO: Getter shouldn't modify state like this!!!
                return TokenType.String;
            }
            
            return TokenType.Syntax;
        }

        private bool IsIdentifierOpener(char c)
        {
            return Char.IsLetter(c) || c == '_';
        }

        private bool IsValidIdentifierCharacter(char c)
        {
            return Char.IsLetterOrDigit(c) || c == '_';
        }
        
        private bool ProcessIdentifier(IList<Token> tokens)
        {
            Token token = MakeToken();
            int next;
            
            while ((next = Peek()) != -1) {
                char nextChar = (char)next;
                                                    
                if (!IsValidIdentifierCharacter(nextChar)) {
                    // Token is finished. Determine whether it is a reserved keyword or a normal identifier. 
                    string value = _valueBuffer.ToString();
                    token.Value = value;
                    
                    if (Keyword.IsKeyword(value)) {
                        token.Type = TokenType.Keyword;
                    } else {
                        token.Type = TokenType.Identifier;
                    }
                    
                    // All done!
                    _valueBuffer.Length = 0;
                    tokens.Add(token);
                    return true;
                }

                _valueBuffer.Append(nextChar);
                Consume();
            }

            // Incomplete.
            return false;
        }

        private bool ProcessNumber(IList<Token> tokens)
        {
            Token token = MakeToken();
            int next;

            while ((next = Peek()) != -1) {
                char nextChar = (char) next;

                if (!(Char.IsDigit(nextChar) || nextChar == '.')) {
                    // Number is finished
                    string value = _valueBuffer.ToString();
                    _valueBuffer.Length = 0;
                    token.Type = TokenType.Number;
                    token.Value = value;
                    tokens.Add(token);
                    return true;
                }

                _valueBuffer.Append(nextChar);
                Consume();
            }
            
            // Incomplete.
            return false;
        }
        
        private bool ProcessString(IList<Token> tokens)
        {
            Token token = MakeToken();
            int next;

            if (!_stringState.ConsumedFirstQuote) {
                Consume();
                _stringState.ConsumedFirstQuote = true;
            }
            
            while ((next = Peek()) != -1) {
                char nextChar = (char) next;

                if (_stringState.IsEscaping) {
                    _valueBuffer.Append(nextChar);
                    Consume();
                    _stringState.IsEscaping = false;
                    
                } else {
                    if (nextChar == '\\') {
                        _stringState.IsEscaping = true;
                        Consume();
                    } else {
                        if (nextChar == '"') {
                            // String finished.
                            Consume();
                            token.Type = TokenType.String;
                            token.Value = _valueBuffer.ToString();
                            _valueBuffer.Length = 0;
                            tokens.Add(token);
                            return true;
                        }

                        _valueBuffer.Append(nextChar);
                        Consume();
                    }
                }
            }
            
            // Incomplete.
            return false;
        }

        private bool ProcessSyntax(IList<Token> tokens)
        {
            Token baseToken = MakeToken();
            
            StringBuilder blockBuilder = new StringBuilder();
            List<Token> blockTokenStubs = new List<Token>(); 
            
            while (GetNextTokenType(ignoreSpace: false) == TokenType.Syntax) {
                int next = Peek();
                char nextChar = (char) next;
                if (Char.IsWhiteSpace(nextChar)) {
                    break;
                }
                blockBuilder.Append((char) next);
                Token stub = MakeToken();
                stub.Type = TokenType.Syntax;
                blockTokenStubs.Add(stub);
                Consume();
            }
            
            string block = blockBuilder.ToString();
            int stubOffset = 0;
            
            while (!string.IsNullOrEmpty(block)) {
                Token token;
                int offset = ProcessSyntaxBlock(blockTokenStubs[stubOffset], block, out token);
                tokens.Add(token);
                stubOffset += offset;
                block = block.Substring(offset);
            }

            return true;
        }

        private int ProcessSyntaxBlock(Token baseToken, string value, out Token lexedToken)
        {
            for (int i = value.Length; i > 1; --i) {
                string subValue = value.Substring(0, i);
                string[] possibleValues = Syntax.GetTokensByLength(i);
                
                for (int j = 0, jlen = possibleValues.Length; j < jlen ; ++j) {
                    if (subValue == possibleValues[j]) {
                        lexedToken = baseToken;
                        lexedToken.Value = subValue;
                        return i;
                    }
                }
            }

            lexedToken = baseToken;
            lexedToken.Value = value.Substring(0, 1);
            return 1;
        }

        private void Consume()
        {
            ++_currentColumn;
            if (Peek() == '\n') {
                ++_currentLine;
                _currentColumn = 0;
            }
            
            ++_sourceIndex;
        }

        private int Peek()
        {
            if (_sourceIndex >= _sourceString.Length) {
                return -1;
            }
            return _sourceString[_sourceIndex];
        }

        private Token MakeToken()
        {
            return new Token {
                Line = _currentLine,
                Column = _currentColumn
            };
        }
    }
}