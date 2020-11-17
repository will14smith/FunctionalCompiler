using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FuncComp.Helpers;
using FuncComp.Language;

namespace FuncComp.GMachine
{
    public class GmCompiler
    {
        private static readonly ImmutableQueue<GmInstruction> InitialCode = ImmutableQueue<GmInstruction>.Empty.Enqueue(new GmInstruction.PushGlobal(new Name("main"))).Enqueue(GmInstruction.Unwind.Instance);

        public GmState Compile(Program<Name> program)
        {
            var (heap, globals) = BuildInitialHeap(Prelude.Program.Supercombinators.Concat(program.Supercombinators));

            return new GmState(InitialCode, ImmutableStack<int>.Empty, heap, globals);
        }

        private (ImmutableDictionary<int, GmNode> Heap, ImmutableDictionary<Name, int> Globals) BuildInitialHeap(IEnumerable<SupercombinatorDefinition<Name>> supercombinatorDefinitions)
        {
            var heap = new Dictionary<int, GmNode>();
            var globals = new Dictionary<Name, int>();

            foreach (var def in supercombinatorDefinitions)
            {
                var addr = heap.Count;

                heap[addr] = new GmNode.Global(def.Parameters.Count, CompileSc(def.Parameters, def.Body));
                globals[def.Name] = addr;
            }

            // TODO primitives

            return (heap.ToImmutableDictionary(), globals.ToImmutableDictionary());
        }

        private ImmutableQueue<GmInstruction> CompileSc(IReadOnlyCollection<Name> parameters, Expression<Name> body)
        {
            var parameterOffsets = parameters.Select((n, i) => (Index: i, Name: n)).ToDictionary(x=> x.Name, x=> x.Index);

            return CompileR(body, parameterOffsets);
        }

        private ImmutableQueue<GmInstruction> CompileR(Expression<Name> expr, IReadOnlyDictionary<Name, int> environment)
        {
            var code = CompileC(expr, environment);

            return code.EnqueueRange(new GmInstruction[] {
                new GmInstruction.Update(environment.Count),
                new GmInstruction.Pop(environment.Count),
                GmInstruction.Unwind.Instance
            });
        }

        private ImmutableQueue<GmInstruction> CompileC(Expression<Name> expr, IReadOnlyDictionary<Name,int> environment)
        {
            var code = ImmutableQueue<GmInstruction>.Empty;

            return expr switch
            {
                Expression<Name>.Variable variable => environment.TryGetValue(variable.Name, out var offset) ? code.Enqueue(new GmInstruction.Push(offset)) : code.Enqueue(new GmInstruction.PushGlobal(variable.Name)),
                Expression<Name>.Number num => code.Enqueue(new GmInstruction.PushInt(num.Value)),
                Expression<Name>.Application ap => code.EnqueueRange(CompileC(ap.Parameter, environment)).EnqueueRange(CompileC(ap.Function, ArgOffset(1, environment))).Enqueue(GmInstruction.MkAp.Instance),

                _ => throw new ArgumentOutOfRangeException(nameof(expr))
            };
        }

        private static IReadOnlyDictionary<Name, int> ArgOffset(int offset, IReadOnlyDictionary<Name,int> environment)
        {
            return environment.ToDictionary(x => x.Key, x => x.Value + offset);
        }
    }
}