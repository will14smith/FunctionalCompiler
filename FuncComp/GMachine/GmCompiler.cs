using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FuncComp.Helpers;
using FuncComp.Language;

namespace FuncComp.GMachine
{
    public partial class GmCompiler
    {
        private static readonly ImmutableQueue<GmInstruction> InitialCode = ImmutableQueue<GmInstruction>.Empty.Enqueue(new GmInstruction.PushGlobal(new Name("main"))).Enqueue(GmInstruction.Eval.Instance);

        public GmState Compile(Program<Name> program)
        {
            var (heap, globals) = BuildInitialHeap(Prelude.Program.Supercombinators.Concat(program.Supercombinators));

            return new GmState(InitialCode, ImmutableStack<int>.Empty, ImmutableStack<(ImmutableQueue<GmInstruction> Code, ImmutableStack<int> Stack)>.Empty, heap, globals);
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

            foreach (var (name, args, insts) in CompiledPrimitives)
            {
                var addr = heap.Count;

                heap[addr] = new GmNode.Global(args, ImmutableQueue<GmInstruction>.Empty.EnqueueRange(insts));
                globals[new Name(name)] = addr;
            }

            return (heap.ToImmutableDictionary(), globals.ToImmutableDictionary());
        }

        private ImmutableQueue<GmInstruction> CompileSc(IReadOnlyCollection<Name> parameters, Expression<Name> body)
        {
            var parameterOffsets = parameters.Select((n, i) => (Index: i, Name: n)).ToImmutableDictionary(x=> x.Name, x=> x.Index);

            return CompileR(body, parameterOffsets);
        }

        private ImmutableQueue<GmInstruction> CompileR(Expression<Name> expr, ImmutableDictionary<Name, int> environment)
        {
            var code = CompileE(expr, environment);

            return code.EnqueueRange(new GmInstruction[] {
                new GmInstruction.Update(environment.Count),
                new GmInstruction.Pop(environment.Count),
                GmInstruction.Unwind.Instance
            });
        }

        private ImmutableQueue<GmInstruction> CompileE(Expression<Name> expr, ImmutableDictionary<Name, int> environment)
        {
            var code = ImmutableQueue<GmInstruction>.Empty;

            return expr switch
            {
                Expression<Name>.Number num => code.Enqueue(new GmInstruction.PushInt(num.Value)),
                Expression<Name>.Let let when !let.IsRecursive => CompileLet(true, let, environment),
                Expression<Name>.Let letRec when letRec.IsRecursive => CompileLetRec(true, letRec, environment),

                Expression<Name>.Application ap when TryCompileEAp(ap, environment, out var result) => code.EnqueueRange(result),

                _ => CompileC(expr, environment).Enqueue(GmInstruction.Eval.Instance),
            };
        }

        private bool TryCompileEAp(Expression<Name>.Application ap, ImmutableDictionary<Name, int> environment, out ImmutableQueue<GmInstruction> instructions)
        {
            instructions = ImmutableQueue<GmInstruction>.Empty;

            if (ap.Function is Expression<Name>.Variable fn1 && fn1.Name.Value == "negate")
            {
                instructions = instructions.EnqueueRange(CompileE(ap.Parameter, environment)).Enqueue(new GmInstruction.Prim(GmInstruction.Prim.PrimType.Neg));
                return true;
            }

            if (ap.Function is Expression<Name>.Application apL && apL.Function is Expression<Name>.Variable fn2 && PrimMapping2.TryGetValue(fn2.Name.Value, out var prim2Type))
            {
                instructions = instructions.EnqueueRange(CompileE(ap.Parameter, environment)).EnqueueRange(CompileE(apL.Parameter, ArgOffset(1, environment))).Enqueue(new GmInstruction.Prim(prim2Type));
                return true;
            }

            if (ap.Function is Expression<Name>.Application apT && apT.Function is Expression<Name>.Application apC && apC.Function is Expression<Name>.Variable fn3 && fn3.Name.Value == "if")
            {
                var trueCode = CompileE(apT.Parameter, environment);
                var falseCode = CompileE(ap.Parameter, environment);

                instructions = instructions
                    .EnqueueRange(CompileE(apC.Parameter, environment))
                    .Enqueue(new GmInstruction.Cond(trueCode, falseCode));

                return true;
            }

            return false;
        }

        private ImmutableQueue<GmInstruction> CompileC(Expression<Name> expr, ImmutableDictionary<Name, int> environment)
        {
            var code = ImmutableQueue<GmInstruction>.Empty;

            return expr switch
            {
                Expression<Name>.Variable variable => environment.TryGetValue(variable.Name, out var offset) ? code.Enqueue(new GmInstruction.Push(offset)) : code.Enqueue(new GmInstruction.PushGlobal(variable.Name)),
                Expression<Name>.Number num => code.Enqueue(new GmInstruction.PushInt(num.Value)),
                Expression<Name>.Application ap => code.EnqueueRange(CompileC(ap.Parameter, environment)).EnqueueRange(CompileC(ap.Function, ArgOffset(1, environment))).Enqueue(GmInstruction.MkAp.Instance),
                Expression<Name>.Let let when !let.IsRecursive => CompileLet(false, let, environment),
                Expression<Name>.Let letRec when letRec.IsRecursive => CompileLetRec(false, letRec, environment),


                _ => throw new ArgumentOutOfRangeException(nameof(expr))
            };
        }

        private ImmutableQueue<GmInstruction> CompileLet(bool isStrict, Expression<Name>.Let let, ImmutableDictionary<Name, int> environment)
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

            var innerInstructions = isStrict ? CompileE(let.Body, innerEnvironment) : CompileC(let.Body, innerEnvironment);
            instructions = instructions.EnqueueRange(innerInstructions).Enqueue(new GmInstruction.Slide(definitionsCount));

            return instructions;
        }

        private ImmutableQueue<GmInstruction> CompileLetRec(bool isStrict, Expression<Name>.Let letRec, ImmutableDictionary<Name, int> environment)
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

            var innerInstructions = isStrict ? CompileE(letRec.Body, environment) : CompileC(letRec.Body, environment);
            instructions = instructions.EnqueueRange(innerInstructions).Enqueue(new GmInstruction.Slide(definitionsCount));

            return instructions;
        }

        private static ImmutableDictionary<Name, int> ArgOffset(int offset, IReadOnlyDictionary<Name, int> environment)
        {
            return environment.ToImmutableDictionary(x => x.Key, x => x.Value + offset);
        }
    }
}