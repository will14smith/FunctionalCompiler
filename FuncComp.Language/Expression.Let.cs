using System;
using System.Collections.Generic;
using System.Linq;

namespace FuncComp.Language
{
    public abstract partial class Expression<TId>
    {
        public class Let : Expression<TId>, IEquatable<Let>
        {
            public Let(bool isRecursive, IReadOnlyCollection<(TId, Expression<TId>)> definitions, Expression<TId> body)
            {
                IsRecursive = isRecursive;
                Definitions = definitions;
                Body = body;
            }

            public bool IsRecursive { get; }
            public IReadOnlyCollection<(TId, Expression<TId>)> Definitions { get; }
            public Expression<TId> Body { get; }

            public bool Equals(Let? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return IsRecursive == other.IsRecursive && Definitions.Equals(other.Definitions) && Body.Equals(other.Body);
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, obj) || obj is Let other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(IsRecursive, Definitions, Body);
            }

            public override string ToString()
            {
                return $"ELet {IsRecursive} [{string.Join(", ", Definitions.Select(x => $"({x.Item1}, {x.Item2})"))}] ({Body})";
            }

            public override TResult Accept<TResult, TState>(IExpressionVisitor<TId, TState, TResult> visitor, TState state)
            {
                return visitor.VisitLet(this, state);
            }

            public static bool operator ==(Let? left, Let? right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Let? left, Let? right)
            {
                return !Equals(left, right);
            }
        }
    }
}