using System;

namespace FuncComp.Language
{
    public abstract partial class Expression<TId>
    {
        public class Constructor : Expression<TId>, IEquatable<Constructor>
        {
            public Constructor(int tag, int arity)
            {
                Tag = tag;
                Arity = arity;
            }

            public int Tag { get; }
            public int Arity { get; }

            public bool Equals(Constructor? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Tag == other.Tag && Arity == other.Arity;
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, obj) || obj is Constructor other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Tag, Arity);
            }

            public static bool operator ==(Constructor? left, Constructor? right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Constructor? left, Constructor? right)
            {
                return !Equals(left, right);
            }

            public override string ToString()
            {
                return $"EConstr {Tag} {Arity}";
            }

            public override TResult Accept<TResult, TState>(IExpressionVisitor<TId, TState, TResult> visitor, TState state)
            {
                return visitor.VisitConstructor(this, state);
            }
        }
    }
}