using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FuncComp.Helpers;
using FuncComp.Language;

namespace FuncComp.TemplateInstantiation
{
    public class TiEvaluator
    {
        public IEnumerable<TiState> Evaluate(TiState state)
        {
            yield return state;

            while (!IsComplete(state))
            {
                state = Step(state);
                yield return state;
            }
        }

        private TiState Step(TiState state)
        {
            var headAddr = state.Stack.Peek();
            var headNode = state.Heap[headAddr];

            return headNode switch
            {
                TiNode.Number number => StepNum(state, number),
                TiNode.Application application => StepAp(state, application),
                TiNode.Supercombinator supercombinator => StepSc(state, supercombinator),

                _ => throw new ArgumentOutOfRangeException(nameof(headNode))
            };
        }

        private TiState StepNum(TiState state, TiNode.Number number)
        {
            throw new Exception("Number applied as function");
        }

        private TiState StepAp(TiState state, TiNode.Application application)
        {
            return state.PushStack(application.Function);
        }

        private TiState StepSc(TiState state, TiNode.Supercombinator supercombinator)
        {
            var (newStack, items) = state.Stack.PopMultiple(supercombinator.Parameters.Count + 1);

            var args = GetArgs(state.Heap, items.Skip(1));
            var argBindings = args.Zip(supercombinator.Parameters).ToDictionary(x => x.Second, x => x.First);
            var env = state.Globals.AddRange(argBindings);
            var (newHeap, resultAddr) = Instantiate(supercombinator.Body, state.Heap, env);

            newStack = newStack.Push(resultAddr);

            return state.WithStackAndHeap(newStack, newHeap);
        }

        private static IEnumerable<int> GetArgs(ImmutableDictionary<int, TiNode> heap, IEnumerable<int> addrs)
        {
            return addrs.Select(addr => heap[addr]).Cast<TiNode.Application>().Select(node => node.Argument);
        }

        private static (ImmutableDictionary<int, TiNode>, int) Instantiate(Expression<Name> expr, ImmutableDictionary<int, TiNode> heap, ImmutableDictionary<Name, int> env)
        {
            switch (expr)
            {
                case Expression<Name>.Number num:
                    return Allocate(heap, new TiNode.Number(num.Value));

                case Expression<Name>.Application ap:
                    var (heap1, a1) = Instantiate(ap.Function, heap, env);
                    var (heap2, a2) = Instantiate(ap.Parameter, heap1, env);

                    return Allocate(heap2, new TiNode.Application(a1, a2));

                case Expression<Name>.Variable variable:
                    return (heap, env[variable.Name]);

                case Expression<Name>.Let let when !let.IsRecursive:
                    var defns = new Dictionary<Name, int>();
                    foreach (var definition in let.Definitions)
                    {
                        var (newHeap, addr) = Instantiate(definition.Item2, heap, env);
                        defns.Add(definition.Item1, addr);
                        heap = newHeap;
                    }

                    var newEnv = env.AddRange(defns);

                    return Instantiate(let.Body, heap, newEnv);

                default: throw new ArgumentOutOfRangeException(nameof(expr));
            }
        }

        private static (ImmutableDictionary<int, TiNode>, int) Allocate(ImmutableDictionary<int,TiNode> heap, TiNode node)
        {
            var addr = heap.Keys.Max() + 1;
            var newHeap = heap.Add(addr, node);

            return (newHeap, addr);
        }

        private static bool IsComplete(TiState state)
        {
            if (state.Stack.IsEmpty)
            {
                throw new Exception("Empty stack");
            }

            var newStack = state.Stack.Pop(out var headAddr);
            if (!newStack.IsEmpty)
            {
                return false;
            }

            var node = state.Heap[headAddr];
            return IsDataNode(node);
        }

        private static bool IsDataNode(TiNode node)
        {
            return node is TiNode.Number;
        }
    }
}