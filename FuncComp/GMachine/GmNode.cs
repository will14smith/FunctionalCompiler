using System.Collections.Immutable;

namespace FuncComp.GMachine
{
    public abstract class GmNode
    {
        public class Number : GmNode
        {
            public Number(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        public class Application : GmNode
        {
            public Application(int function, int argument)
            {
                Function = function;
                Argument = argument;
            }

            public int Function { get; }
            public int Argument { get; }
        }

        public class Global : GmNode
        {
            public Global(int argCount, ImmutableQueue<GmInstruction> code)
            {
                ArgCount = argCount;
                Code = code;
            }

            public int ArgCount { get; }
            public ImmutableQueue<GmInstruction> Code { get; }
        }

        public class Indirection : GmNode
        {
            public Indirection(int addr)
            {
                Addr = addr;
            }

            public int Addr { get; }
        }
    }
}