using System;
using System.Collections.Generic;

namespace FuncComp.Language
{
    public class Program<TId>
    {
        public Program(IReadOnlyCollection<SupercombinatorDefinition<TId>> supercombinators)
        {
            Supercombinators = supercombinators;
        }

        public IReadOnlyCollection<SupercombinatorDefinition<TId>> Supercombinators { get; }
    }

    public class SupercombinatorDefinition<TId> : IEquatable<SupercombinatorDefinition<TId>>
    {
        public SupercombinatorDefinition(Name name, IReadOnlyCollection<TId> parameters, Expression<TId> body)
        {
            Name = name;
            Parameters = parameters;
            Body = body;
        }

        public Name Name { get; }
        public IReadOnlyCollection<TId> Parameters { get; }
        public Expression<TId> Body { get; }

        public bool Equals(SupercombinatorDefinition<TId>? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name.Equals(other.Name) && Parameters.Equals(other.Parameters) && Body.Equals(other.Body);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is SupercombinatorDefinition<TId> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Parameters, Body);
        }

        public override string ToString()
        {
            return $"ScDefn {Name} [{string.Join(", ", Parameters)}] ({Body})";
        }

        public static bool operator ==(SupercombinatorDefinition<TId>? left, SupercombinatorDefinition<TId>? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SupercombinatorDefinition<TId>? left, SupercombinatorDefinition<TId>? right)
        {
            return !Equals(left, right);
        }
    }
}