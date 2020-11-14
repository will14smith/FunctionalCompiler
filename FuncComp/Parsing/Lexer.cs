using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace FuncComp.Parsing
{
    public class Lexer
    {
        public IEnumerable<Token> Lex(string input)
        {
            var state = new State(input);

            while (!state.IsEof)
            {
                if (char.IsWhiteSpace(state.Current))
                {
                    state.Offset++;
                }
                else if (char.IsDigit(state.Current))
                {
                    yield return LexNumber(ref state);
                }
                else if (char.IsLetter(state.Current))
                {
                    yield return LexIdentifier(ref state);
                }
                else if (state.TryGetN(2, out var value) && value == "||")
                {
                    while (!state.IsEof && state.Current != '\n') state.Offset++;
                }
                else
                {
                    yield return LexToken(ref state);
                }
            }
        }

        private Token LexNumber(ref State state)
        {
            var start = state.Offset;

            while (!state.IsEof && char.IsDigit(state.Current)) state.Offset++;

            var value = state.Input.Substring(start, state.Offset - start);

            return new Token(TokenType.Number, value);
        }

        private Token LexIdentifier(ref State state)
        {
            var start = state.Offset;

            while (!state.IsEof && IsIdentifierChar(state.Current)) state.Offset++;

            var value = state.Input.Substring(start, state.Offset - start);

            return new Token(TokenType.Identifier, value);
        }

        private Token LexToken(ref State state)
        {
            var twoCharMatch = state.TryGetN(2, out var twoCharValue);

            if (twoCharMatch && (twoCharValue == "==" || twoCharValue == "~=" || twoCharValue == ">=" || twoCharValue == "<=" || twoCharValue == "->"))
            {
                state.Offset += 2;

                return new Token(TokenType.Other, twoCharValue);
            }

            var value = state.Current.ToString();
            state.Offset++;
            return new Token(TokenType.Other, value);
        }

        private static bool IsIdentifierChar(in char c) => char.IsLetter(c) || char.IsDigit(c) || c == '_';

        private struct State
        {
            public string Input { get; }

            public char Current => Get(Offset);
            public char Get(int offset) => Input[offset];
            public bool TryGetN(int length, out string value) => TryGetN(Offset, length, out value);
            public bool IsEof => IsOffsetEof(Offset);
            public bool IsOffsetEof(int offset) => offset >= Input.Length;

            public int Offset;

            public State(string input)
            {
                Input = input;
                Offset = 0;
            }

            public bool TryGetN(int offset, int length, out string value)
            {
                if (IsOffsetEof(offset + length - 1))
                {
                    value = "";
                    return false;
                }

                value = Input.Substring(offset, length);
                return true;
            }
        }
    }
}