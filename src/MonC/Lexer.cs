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
                    token = new Token();
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
            token = new Token();
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
            token = new Token();
            int next;

            while ((next = Peek()) != -1) {
                char nextChar = (char) next;

                if (!(Char.IsDigit(nextChar) || nextChar == '.')) {
                    // Number is finished
                    string value = _valueBuffer.ToString();
                    _valueBuffer.Length = 0;
                    token = new Token {Type = TokenType.Number, Value = value};
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
            token = new Token();
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
                            token = new Token {Type = TokenType.String, Value = _valueBuffer.ToString()};
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
            token = new Token();
            int next = Peek();

            if (next == -1) {
                // Incomplete.
                return false;
            }
            
            token = new Token {Type = TokenType.Syntax, Value = ((char) next).ToString()};
            Consume();
            return true;
        }

        private void Consume()
        {
            ++_sourceIndex;
        }

        private int Peek()
        {
            if (_sourceIndex >= _sourceString.Length) {
                return -1;
            }
            return _sourceString[_sourceIndex];
        }

    }
}