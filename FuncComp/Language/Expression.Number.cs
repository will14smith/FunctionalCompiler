using System;

namespace FuncComp.Language
{
    public abstract partial class Expression<TId>
    {
        public class Number : Expression<TId>, IEquatable<Number>
        {
            public Number(int value)
            {
                Value = value;
            }

            public int Value { get; }

            public bool Equals(Number? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Value == other.Value;
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, obj) || obj is Number other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Value;
            }

            public override string ToString()
            {
                return $"{Value}";
            }

            public override TResult Accept<TResult, TState>(IExpressionVisitor<TId, TState, TResult> visitor, TState state)
            {
                return visitor.VisitNumber(this, state);
            }

            public static bool operator ==(Number? left, Number? right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Number? left, Number? right)
            {
                return !Equals(left, right);
            }
        }
    }
}