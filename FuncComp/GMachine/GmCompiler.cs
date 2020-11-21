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
            var parameterOffsets = parameters.Select((n, i) => (Index: i, Name: n)).ToImmutableDictionary(x=> x.Name, x=> x.Index);

            return CompileR(body, parameterOffsets);
        }

        private ImmutableQueue<GmInstruction> CompileR(Expression<Name> expr, ImmutableDictionary<Name, int> environment)
        {
            var code = CompileC(expr, environment);

            return code.EnqueueRange(new GmInstruction[] {
                new GmInstruction.Update(environment.Count),
                new GmInstruction.Pop(environment.Count),
                GmInstruction.Unwind.Instance
            });
        }

        private ImmutableQueue<GmInstruction> CompileC(Expression<Name> expr, ImmutableDictionary<Name, int> environment)
        {
            var code = ImmutableQueue<GmInstruction>.Empty;

            return expr switch
            {
                Expression<Name>.Variable variable => environment.TryGetValue(variable.Name, out var offset) ? code.Enqueue(new GmInstruction.Push(offset)) : code.Enqueue(new GmInstruction.PushGlobal(variable.Name)),
                Expression<Name>.Number num => code.Enqueue(new GmInstruction.PushInt(num.Value)),
                Expression<Name>.Application ap => code.EnqueueRange(CompileC(ap.Parameter, environment)).EnqueueRange(CompileC(ap.Function, ArgOffset(1, environment))).Enqueue(GmInstruction.MkAp.Instance),
                Expression<Name>.Let let when !let.IsRecursive => CompileCLet(let, environment),
                Expression<Name>.Let letRec when letRec.IsRecursive => CompileCLetRec(letRec, environment),


                _ => throw new ArgumentOutOfRangeException(nameof(expr))
            };
        }

        private ImmutableQueue<GmInstruction> CompileCLet(Expression<Name>.Let let, ImmutableDictionary<Name, int> environment)
        {
            var definitionsCount = let.Definitions.Count;

            var innerEnvironment = ArgOffset(definitionsCount, environment);
            var instructions = ImmutableQueue<GmInstruction>.Empty;

            var offset = 0;
            foreach (var (name, defnExpr) in let.Definitions)
            {
                var defnInstructions = CompileC(defnExpr, ArgOffset(offset++, environment));
                innerEnvironment = innerEnvironment.SetItem(name, definitionsCount - offset);
                instructions = instructions.EnqueueRange(defnInstructions);
            }

            var innerInstructions = CompileC(let.Body, innerEnvironment);
            instructions = instructions.EnqueueRange(innerInstructions).Enqueue(new GmInstruction.Slide(definitionsCount));

            return instructions;
        }

        private ImmutableQueue<GmInstruction> CompileCLetRec(Expression<Name>.Let letRec, ImmutableDictionary<Name, int> environment)
        {
            var definitionsCount = letRec.Definitions.Count;

            environment = ArgOffset(definitionsCount, environment);

            var offset = 0;
            foreach (var (name, _) in letRec.Definitions)
            {
                var targetOffset = definitionsCount - ++offset;
                environment = environment.SetItem(name, targetOffset);
            }


            var instructions = ImmutableQueue<GmInstruction>.Empty.Enqueue(new GmInstruction.Alloc(definitionsCount));

            offset = 0;
            foreach (var (_, defnExpr) in letRec.Definitions)
            {
                var defnInstructions = CompileC(defnExpr, environment);
                var targetOffset = definitionsCount - ++offset;
                instructions = instructions.EnqueueRange(defnInstructions).Enqueue(new GmInstruction.Update(targetOffset));
            }

            var innerInstructions = CompileC(letRec.Body, environment);
            instructions = instructions.EnqueueRange(innerInstructions).Enqueue(new GmInstruction.Slide(definitionsCount));

            return instructions;
        }

        private static ImmutableDictionary<Name, int> ArgOffset(int offset, IReadOnlyDictionary<Name, int> environment)
        {
            return environment.ToImmutableDictionary(x => x.Key, x => x.Value + offset);
        }
    }
}