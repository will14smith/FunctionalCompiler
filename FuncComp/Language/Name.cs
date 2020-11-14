using System;

namespace FuncComp.Language
{
    public class Name : IEquatable<Name>
    {
        public Name(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public bool Equals(Name? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is Name other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(Name left, Name right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Name left, Name right)
        {
            return !Equals(left, right);
        }
    }
}