using System.Collections.Generic;
using System.Collections.Immutable;
using FuncComp.TemplateInstantiation;

namespace FuncComp.GMachine
{
    public partial class GmCompiler
    {
        static GmCompiler()
        {
            var compiledPrimitives = new List<(string, int, IReadOnlyCollection<GmInstruction>)>
            {
                Prim1("negate", GmInstruction.Prim.PrimType.Neg),
                If(),
            };

            foreach (var (name, type) in PrimMapping2)
            {
                compiledPrimitives.Add(Prim2(name, type));
            }

            CompiledPrimitives = compiledPrimitives;
        }

        private static readonly IReadOnlyDictionary<string, GmInstruction.Prim.PrimType> PrimMapping2 = new Dictionary<string, GmInstruction.Prim.PrimType>
        {
            { "+", GmInstruction.Prim.PrimType.Add },
            { "-", GmInstruction.Prim.PrimType.Sub },
            { "*", GmInstruction.Prim.PrimType.Mul },
            { "/", GmInstruction.Prim.PrimType.Div },

            { "==", GmInstruction.Prim.PrimType.Eq },
            { "~=", GmInstruction.Prim.PrimType.Ne },
            { "<", GmInstruction.Prim.PrimType.Lt },
            { "<=", GmInstruction.Prim.PrimType.Le },
            { ">", GmInstruction.Prim.PrimType.Gt },
            { ">=", GmInstruction.Prim.PrimType.Ge },
        };

        private static readonly IReadOnlyCollection<(string Name, int Args, IReadOnlyCollection<GmInstruction> Instructions)> CompiledPrimitives;

        private static (string, int, IReadOnlyCollection<GmInstruction>) Prim1(string name, GmInstruction.Prim.PrimType type)
        {
            return (name, 1, new GmInstruction[]
            {
                new GmInstruction.Push(0),
                GmInstruction.Eval.Instance,
                new GmInstruction.Prim(type),
                new GmInstruction.Update(1),
                new GmInstruction.Pop(1),
                GmInstruction.Unwind.Instance
            });
        }

        private static (string, int, IReadOnlyCollection<GmInstruction>) Prim2(string name, GmInstruction.Prim.PrimType type)
        {
            return (name, 2, new GmInstruction[]
            {
                new GmInstruction.Push(1),
                GmInstruction.Eval.Instance,
                new GmInstruction.Push(1),
                GmInstruction.Eval.Instance,
                new GmInstruction.Prim(type),
                new GmInstruction.Update(2),
                new GmInstruction.Pop(2),
                GmInstruction.Unwind.Instance
            });
        }

        private static (string, int, IReadOnlyCollection<GmInstruction>) If()
        {
            return ("if", 3, new GmInstruction[]
            {
                new GmInstruction.Push(0),
                GmInstruction.Eval.Instance,
                new GmInstruction.Cond(ImmutableQueue<GmInstruction>.Empty.Enqueue(new GmInstruction.Push(1)), ImmutableQueue<GmInstruction>.Empty.Enqueue(new GmInstruction.Push(2))),
                new GmInstruction.Update(3),
                new GmInstruction.Pop(3),
                GmInstruction.Unwind.Instance,
            });
        }
    }
}