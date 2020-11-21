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
        private static readonly ImmutableQueue<GmInstruction> InitialCode = ImmutableQueue<GmInstruction>.Empty.Enqueue(new GmInstruction.PushGlobal(new Name("main"))).Enqueue(GmInstruction.Eval.Instance);

        private static readonly IReadOnlyCollection<(string Name, int Args, IReadOnlyCollection<GmInstruction> Instructions)> CompiledPrimitives = new []
        {
            Prim1("negate", GmInstruction.Prim.PrimType.Neg),

            Prim2("+", GmInstruction.Prim.PrimType.Add),
            Prim2("-", GmInstruction.Prim.PrimType.Sub),
            Prim2("*", GmInstruction.Prim.PrimType.Mul),
            Prim2("/", GmInstruction.Prim.PrimType.Div),

            Prim2("==", GmInstruction.Prim.PrimType.Eq),
            Prim2("~=", GmInstruction.Prim.PrimType.Ne),
            Prim2("<", GmInstruction.Prim.PrimType.Lt),
            Prim2("<=", GmInstruction.Prim.PrimType.Le),
            Prim2(">", GmInstruction.Prim.PrimType.Gt),
            Prim2(">=", GmInstruction.Prim.PrimType.Ge),

            If(),
        };

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