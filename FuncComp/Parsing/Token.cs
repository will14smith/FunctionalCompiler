namespace FuncComp.Parsing
{
    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }

        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }
    }

    public enum TokenType
    {
        Number,
        Identifier,
        Other
    }
}