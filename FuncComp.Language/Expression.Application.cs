using System;

namespace FuncComp.Language
{
    public abstract partial class Expression<TId>
    {
        public class Application : Expression<TId>, IEquatable<Application>
        {
            public Application(Expression<TId> function, Expression<TId> parameter)
            {
                Function = function;
                Parameter = parameter;
            }

            public Expression<TId> Function { get; }
            public Expression<TId> Parameter { get; }

            public bool Equals(Application? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Function.Equals(other.Function) && Parameter.Equals(other.Parameter);
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, obj) || obj is Application other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Function, Parameter);
            }

            public override string ToString()
            {
                return $"EAp ({Function}) ({Parameter})";
            }

            public override TResult Accept<TResult, TState>(IExpressionVisitor<TId, TState, TResult> visitor, TState state)
            {
                return visitor.VisitApplication(this, state);
            }

            public static bool operator ==(Application? left, Application? right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Application? left, Application? right)
            {
                return !Equals(left, right);
            }
        }
    }
}