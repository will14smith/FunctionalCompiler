using System;
using FuncComp.Language;

namespace FuncComp.GMachine
{
    public abstract class GmInstruction
    {
        public abstract override bool Equals(object? obj);
        public abstract override int GetHashCode();

        public class Unwind : GmInstruction
        {
            public static Unwind Instance { get; } = new Unwind();
            private Unwind() { }

            public override bool Equals(object? obj) => obj is Unwind;
            public override int GetHashCode() => 13;
        }

        public class PushGlobal : GmInstruction
        {
            public PushGlobal(Name name)
            {
                Name = name;
            }

            public Name Name { get; }

            public override bool Equals(object? obj) => obj is PushGlobal other && Name == other.Name;
            public override int GetHashCode() => HashCode.Combine(17, Name);
        }

        public class PushInt : GmInstruction
        {
            public PushInt(int value)
            {
                Value = value;
            }

            public int Value { get; }

            public override bool Equals(object? obj) => obj is PushInt other && Value == other.Value;
            public override int GetHashCode() => HashCode.Combine(19, Value);
        }

        public class Push : GmInstruction
        {
            public Push(int offset)
            {
                Offset = offset;
            }

            public int Offset { get; }

            public override bool Equals(object? obj) => obj is Push other && Offset == other.Offset;
            public override int GetHashCode() => HashCode.Combine(23, Offset);
        }

        public class MkAp : GmInstruction
        {
            public static MkAp Instance { get; } = new MkAp();
            private MkAp() { }

            public override bool Equals(object? obj) => obj is MkAp;
            public override int GetHashCode() => 29;
        }

        public class Slide : GmInstruction
        {
            public Slide(int count)
            {
                Count = count;
            }

            public int Count { get; }

            public override bool Equals(object? obj) => obj is Slide other && Count == other.Count;
            public override int GetHashCode() => HashCode.Combine(31, Count);
        }
    }
}