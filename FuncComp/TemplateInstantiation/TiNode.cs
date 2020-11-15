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

        public class Data : TiNode
        {
            public Data(int tag, IReadOnlyList<int> components)
            {
                Tag = tag;
                Components = components;
            }

            public int Tag { get; }
            public IReadOnlyList<int> Components { get; }
        }
    }

    public abstract class PrimitiveType
    {
        public class Constructor : PrimitiveType
        {
            public Constructor(int tag, int arity)
            {
                Tag = tag;
                Arity = arity;
            }

            public int Tag { get; }
            public int Arity { get; }
        }


        public class Neg : PrimitiveType { }
        public class Add : PrimitiveType { }
        public class Sub : PrimitiveType { }
        public class Mul : PrimitiveType { }
        public class Div : PrimitiveType { }

        public class Greater : PrimitiveType { }
        public class GreaterEqual : PrimitiveType { }
        public class Less : PrimitiveType { }
        public class LessEqual : PrimitiveType { }
        public class Equal : PrimitiveType { }
        public class NotEqual : PrimitiveType { }

        public class If : PrimitiveType { }
        public class CasePair : PrimitiveType { }
    }
}