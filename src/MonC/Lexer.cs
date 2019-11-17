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

        private bool _isCurrentCommentTypeKnown;
        private bool _isCurrentCommentMultiline;

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
                    tokens.Add(new Token(TokenType.None, "", GetCurrentLocation()));
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
                case TokenType.Comment:
                    canContinue = ProcessComment();
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
            if (IsNextTokenCommentOpener()) {
                return TokenType.Comment;
            }
            
            return TokenType.Syntax;
        }

        private bool IsIdentifierOpener(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private bool IsValidIdentifierCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        private bool IsValidNumericCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || c == '.';
        }

        private bool IsNextTokenCommentOpener()
        {
            return IsNextTokenSingleLineComment() || IsNextTokenMultiLineOpener();
        }

        private bool IsNextTokenSingleLineComment()
        {
            return Peek(0) == '/' && Peek(1) == '/';
        }

        private bool IsNextTokenMultiLineOpener()
        {
            return Peek(0) == '/' && Peek(1) == '*';
        }

        private bool IsNextTokenMultiLineCloser()
        {
            return Peek(0) == '/' && Peek(1) == '*';
        }

        private void ConsumeMultiLineCloser()
        {
            Consume();
            Consume();
        }

        private bool ProcessIdentifier(IList<Token> tokens)
        {
            FileLocation location = GetCurrentLocation();
            int next;
            
            while ((next = Peek()) != -1) {
                char nextChar = (char)next;
                                                    
                if (!IsValidIdentifierCharacter(nextChar)) {
                    // Token is finished. Determine whether it is a reserved keyword or a normal identifier. 
                    string value = _valueBuffer.ToString();
                    TokenType type;
                    
                    if (Keyword.IsKeyword(value)) {
                        type = TokenType.Keyword;
                    } else {
                        type = TokenType.Identifier;
                    }
                    
                    // All done!
                    _valueBuffer.Length = 0;
                    tokens.Add(new Token(type, value, location));
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
            FileLocation location = GetCurrentLocation();
            int next;

            while ((next = Peek()) != -1) {
                char nextChar = (char) next;

                if (!IsValidNumericCharacter(nextChar)) {
                    // Number is finished
                    string value = _valueBuffer.ToString();
                    _valueBuffer.Length = 0;
                    tokens.Add(new Token(TokenType.Number, value, location));
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
            FileLocation location = GetCurrentLocation();
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
                            tokens.Add(new Token(TokenType.String, _valueBuffer.ToString(), location));
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

        private bool ProcessSyntax(IList<Token> tokens)
        {
            StringBuilder blockBuilder = new StringBuilder();
            List<FileLocation> blockTokenLocations = new List<FileLocation>(); 
            
            while (GetNextTokenType(ignoreSpace: false) == TokenType.Syntax) {
                int next = Peek();
                char nextChar = (char) next;
                if (Char.IsWhiteSpace(nextChar)) {
                    break;
                }
                blockBuilder.Append((char) next);
                blockTokenLocations.Add(GetCurrentLocation());
                Consume();
            }
            
            string block = blockBuilder.ToString();
            int stubOffset = 0;
            
            while (!string.IsNullOrEmpty(block)) {
                Token token;
                int offset = ProcessSyntaxBlock(blockTokenLocations[stubOffset], block, out token);
                tokens.Add(token);
                stubOffset += offset;
                block = block.Substring(offset);
            }

            return true;
        }

        private int ProcessSyntaxBlock(FileLocation location, string value, out Token lexedToken)
        {
            for (int i = value.Length; i > 1; --i) {
                string subValue = value.Substring(0, i);
                string[] possibleValues = Syntax.GetTokensByLength(i);
                
                for (int j = 0, jlen = possibleValues.Length; j < jlen ; ++j) {
                    if (subValue == possibleValues[j]) {
                        lexedToken = new Token(TokenType.Syntax, subValue, location);
                        return i;
                    }
                }
            }
            
            lexedToken = new Token(TokenType.Syntax, value.Substring(0, 1), location);
            return 1;
        }

        private bool ProcessComment()
        {
            if (!_isCurrentCommentTypeKnown) {
                _isCurrentCommentMultiline = IsNextTokenMultiLineOpener();
                _isCurrentCommentTypeKnown = true;
            }
            
            while (true) {
                int val = Peek();
                char c = (char)val;

                if (val == -1) {
                    return false;
                }
                if (_isCurrentCommentMultiline) {
                    if (IsNextTokenMultiLineCloser()) {
                        ConsumeMultiLineCloser();
                        break;
                    }
                } else { 
                    if (c == '\n') {
                        Consume();
                        break;
                    }
                }

                Consume();
            }

            _isCurrentCommentTypeKnown = false;
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

        private int Peek(int offset = 0)
        {
            int index = _sourceIndex + offset;
            if (index >= _sourceString.Length) {
                return -1;
            }
            return _sourceString[index];
        }

//        private Token MakeToken()
//        {
//            return new Token {
//                Location = GetCurrentLocation()
//            };
//        }

        

        private FileLocation GetCurrentLocation()
        {
            return new FileLocation {Line = _currentLine, Column = _currentColumn};
        }
    }
}