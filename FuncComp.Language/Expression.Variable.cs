using System;

namespace FuncComp.Language
{
    public abstract partial class Expression<TId>
    {
        public class Variable : Expression<TId>, IEquatable<Variable>
        {
            // TODO should this be TId rather than Name?
            // the book says it should be name but...
            public Variable(Name name)
            {
                Name = name;
            }

            public Name Name { get; }

            public bool Equals(Variable? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Name.Equals(other.Name);
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, obj) || obj is Variable other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }

            public override string ToString()
            {
                return $"{Name}";
            }

            public override TResult Accept<TResult, TState>(IExpressionVisitor<TId, TState, TResult> visitor, TState state)
            {
                return visitor.VisitVariable(this, state);
            }

            public static bool operator ==(Variable? left, Variable? right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Variable? left, Variable? right)
            {
                return !Equals(left, right);
            }
        }
    }
}