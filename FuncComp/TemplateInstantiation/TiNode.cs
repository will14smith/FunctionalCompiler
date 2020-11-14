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
    }
}