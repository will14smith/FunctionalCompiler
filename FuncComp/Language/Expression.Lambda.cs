using System;
using System.Collections.Generic;

namespace FuncComp.Language
{
    public abstract partial class Expression<TId>
    {
        public class Lambda : Expression<TId>, IEquatable<Lambda>
        {
            public Lambda(IReadOnlyCollection<TId> parameters, Expression<TId> body)
            {
                Parameters = parameters;
                Body = body;
            }

            public IReadOnlyCollection<TId> Parameters { get; }
            public Expression<TId> Body { get; }

            public bool Equals(Lambda? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Parameters.Equals(other.Parameters) && ((Object) Body).Equals(other.Body);
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, obj) || obj is Lambda other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Parameters, Body);
            }

            public static bool operator ==(Lambda? left, Lambda? right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Lambda? left, Lambda? right)
            {
                return !Equals(left, right);
            }

            public override string ToString()
            {
                return $"ELam [{string.Join(", ", Parameters)}] ({Body})";
            }

            public override TResult Accept<TResult, TState>(IExpressionVisitor<TId, TState, TResult> visitor, TState state)
            {
                return visitor.VisitLambda(this, state);
            }
        }
    }
}