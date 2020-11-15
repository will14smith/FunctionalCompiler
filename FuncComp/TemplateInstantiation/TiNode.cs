using System.Collections.Generic;
using FuncComp.Language;

namespace FuncComp.TemplateInstantiation
{
    public abstract class TiNode
    {
        public class Application : TiNode
        {
            public Application(int function, int argument)
            {
                Function = function;
                Argument = argument;
            }

            public int Function { get; }
            public int Argument { get; }
        }

        public class Supercombinator : TiNode
        {
            public Supercombinator(Name name, IReadOnlyCollection<Name> parameters, Expression<Name> body)
            {
                Name = name;
                Parameters = parameters;
                Body = body;
            }

            public Name Name { get; }
            public IReadOnlyCollection<Name> Parameters { get; }
            public Expression<Name> Body { get; }
        }

        public class Number : TiNode
        {
            public Number(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        public class Indirection : TiNode
        {
            public Indirection(int address)
            {
                Address = address;
            }

            public int Address { get; }
        }

        public class Primitive : TiNode
        {
            public Primitive(Name name, PrimitiveType type)
            {
                Name = name;
                Type = type;
            }

            public Name Name { get; }
            public PrimitiveType Type { get; }
        }
    }

    public enum PrimitiveType
    {
        Neg,
        Add,
        Sub,
        Mul,
        Div,
    }
}