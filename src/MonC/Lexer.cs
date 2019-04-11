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
            
            Token token;
            
            while (Lex(out token)) {
                tokens.Add(token);
            }
        }

        private bool Lex(out Token token)
        {
            if (_currentTokenType == TokenType.None) {
                _currentTokenType = GetNextTokenType();
            }

            bool canContinue;
            
            switch (_currentTokenType) {
                default:
                    token = MakeToken();
                    canContinue = false;
                    break;
                case TokenType.Identifier:
                    canContinue = ProcessIdentifier(out token);
                    break;
                case TokenType.Number:
                    canContinue = ProcessNumber(out token);
                    break;
                case TokenType.String:
                    canContinue = ProcessString(out token);
                    break;
                case TokenType.Syntax:
                    canContinue = ProcessSyntax(out token);
                    break;
            }

            if (canContinue) {
                _currentTokenType = TokenType.None;
            }
            return canContinue;
        }

        private TokenType GetNextTokenType()
        {
            int next;
            char nextChar;

            // Skip non-token characters
            while ((next = Peek()) != -1) {
                nextChar = (char) next;
                if (Char.IsWhiteSpace(nextChar)) {
                    Consume();
                } else {
                    break;
                }
            }
            
            if (next == -1 || next == '\0') {
                return TokenType.None;
            }

            nextChar = (char)next;
            
            if (Char.IsLetter(nextChar)) {
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
        
        private bool ProcessIdentifier(out Token token)
        {
            token = MakeToken();
            int next;
            
            while ((next = Peek()) != -1) {
                char nextChar = (char)next;
                                                    
                if (!Char.IsLetterOrDigit(nextChar)) {
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
                    return true;
                }

                _valueBuffer.Append(nextChar);
                Consume();
            }

            // Incomplete.
            return false;
        }

        private bool ProcessNumber(out Token token)
        {
            token = MakeToken();
            int next;

            while ((next = Peek()) != -1) {
                char nextChar = (char) next;

                if (!(Char.IsDigit(nextChar) || nextChar == '.')) {
                    // Number is finished
                    string value = _valueBuffer.ToString();
                    _valueBuffer.Length = 0;
                    token.Type = TokenType.Number;
                    token.Value = value;
                    return true;
                }

                _valueBuffer.Append(nextChar);
                Consume();
            }
            
            // Incomplete.
            return false;
        }
        
        private bool ProcessString(out Token token)
        {
            token = MakeToken();
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

        private bool ProcessSyntax(out Token token)
        {
            token = MakeToken();
            int next = Peek();

            if (next == -1) {
                // Incomplete.
                return false;
            }

            token.Type = TokenType.Syntax;
            token.Value = ((char) next).ToString();
            Consume();
            return true;
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