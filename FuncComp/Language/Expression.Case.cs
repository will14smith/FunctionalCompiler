using System;
using System.Collections.Generic;

namespace FuncComp.Language
{
    public abstract partial class Expression<TId>
    {
        public class Case : Expression<TId>, IEquatable<Case>
        {
            public Case(Expression<TId> match, IReadOnlyCollection<Alternative<TId>> alternatives)
            {
                Match = match;
                Alternatives = alternatives;
            }

            public Expression<TId> Match { get; }
            public IReadOnlyCollection<Alternative<TId>> Alternatives { get; }

            public bool Equals(Case? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Match.Equals(other.Match) && Alternatives.Equals(other.Alternatives);
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, obj) || obj is Case other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Match, Alternatives);
            }

            public override string ToString()
            {
                return $"ECase ({Match}) [{string.Join(", ", Alternatives)}]";
            }

            public override TResult Accept<TResult, TState>(IExpressionVisitor<TId, TState, TResult> visitor, TState state)
            {
                return visitor.VisitCase(this, state);
            }

            public static bool operator ==(Case? left, Case? right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Case? left, Case? right)
            {
                return !Equals(left, right);
            }
        }
    }
}