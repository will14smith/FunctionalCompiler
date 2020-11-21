using System;
using System.Collections.Immutable;
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

        public class Update : GmInstruction
        {
            public Update(int offset)
            {
                Offset = offset;
            }

            public int Offset { get; }

            public override bool Equals(object? obj) => obj is Update other && Offset == other.Offset;
            public override int GetHashCode() => HashCode.Combine(31, Offset);
        }

        public class Pop : GmInstruction
        {
            public Pop(int count)
            {
                Count = count;
            }

            public int Count { get; }

            public override bool Equals(object? obj) => obj is Pop other && Count == other.Count;
            public override int GetHashCode() => HashCode.Combine(37, Count);
        }

        public class Slide : GmInstruction
        {
            public Slide(int count)
            {
                Count = count;
            }

            public int Count { get; }

            public override bool Equals(object? obj) => obj is Slide other && Count == other.Count;
            public override int GetHashCode() => HashCode.Combine(39, Count);
        }

        public class Alloc : GmInstruction
        {
            public Alloc(int count)
            {
                Count = count;
            }

            public int Count { get; }

            public override bool Equals(object? obj) => obj is Alloc other && Count == other.Count;
            public override int GetHashCode() => HashCode.Combine(41, Count);
        }


        public class Eval : GmInstruction
        {
            public static Eval Instance { get; } = new Eval();
            private Eval() { }

            public override bool Equals(object? obj) => obj is Eval;
            public override int GetHashCode() => 43;
        }

        public class Prim : GmInstruction
        {
            public Prim(PrimType type)
            {
                Type = type;
            }

            public enum PrimType
            {
                Add, Sub,
                Mul, Div,
                Neg,
                Eq, Ne,
                Lt, Le,
                Gt, Ge
            }

            public PrimType Type { get; }

            public override bool Equals(object? obj) => obj is Prim other && Type == other.Type;
            public override int GetHashCode() => HashCode.Combine(47, Type);
        }

        public class Cond : GmInstruction
        {
            public Cond(ImmutableQueue<GmInstruction> trueCode, ImmutableQueue<GmInstruction> falseCode)
            {
                TrueCode = trueCode;
                FalseCode = falseCode;
            }

            public ImmutableQueue<GmInstruction> TrueCode { get; }
            public ImmutableQueue<GmInstruction> FalseCode { get; }

            public override bool Equals(object? obj) => obj is Cond other && TrueCode == other.TrueCode && FalseCode == other.FalseCode;
            public override int GetHashCode() => HashCode.Combine(51, TrueCode, FalseCode);
        }
    }
}