using System;
using System.Collections.Generic;

namespace FuncComp.Language
{
    public class Alternative<TId> : IEquatable<Alternative<TId>>
    {
        public Alternative(int tag, IReadOnlyCollection<TId> parameters, Expression<TId> body)
        {
            Tag = tag;
            Parameters = parameters;
            Body = body;
        }

        public int Tag { get; }
        public IReadOnlyCollection<TId> Parameters { get; }
        public Expression<TId> Body { get; }

        public bool Equals(Alternative<TId>? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Tag == other.Tag && Parameters.Equals(other.Parameters) && Body.Equals(other.Body);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is Alternative<TId> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Tag, Parameters, Body);
        }

        public override string ToString()
        {
            return $"Alter {Tag} [{string.Join(", ", Parameters)}] ({Body})";
        }

        public static bool operator ==(Alternative<TId>? left, Alternative<TId>? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Alternative<TId>? left, Alternative<TId>? right)
        {
            return !Equals(left, right);
        }
    }
}