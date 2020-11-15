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
                TiNode.Indirection indirection => StepInd(state, indirection),
                TiNode.Primitive primitive => StepPrim(state, primitive),

                _ => throw new ArgumentOutOfRangeException(nameof(headNode))
            };
        }

        private TiState StepNum(TiState state, TiNode.Number number)
        {
            var stackOnlyHasNumber = state.Stack.Pop().IsEmpty;
            var dumpIsEmpty = state.Dump.IsEmpty;

            if (!stackOnlyHasNumber || dumpIsEmpty)
            {
                throw new Exception("Number applied as function");
            }

            return state.PopDump();
        }

        private TiState StepAp(TiState state, TiNode.Application application)
        {
            var argAddr = application.Argument;
            var arg = state.Heap[argAddr];

            if (arg is TiNode.Indirection argInd)
            {
                var apAddr = state.Stack.Peek();

                var newHeap = state.Heap.SetItem(apAddr, new TiNode.Application(application.Function, argInd.Address));
                return state.WithHeap(newHeap);
            }

            return state.PushStack(application.Function);
        }

        private TiState StepSc(TiState state, TiNode.Supercombinator supercombinator)
        {
            var (newStack, args, root) = GetArgs(state, supercombinator.Parameters.Count);
            var argBindings = args.Zip(supercombinator.Parameters).ToDictionary(x => x.Second, x => x.First);
            var env = state.Globals.SetItems(argBindings);
            var (newHeap, resultAddr) = Instantiate(supercombinator.Body, state.Heap, env, root);

            newStack = newStack.Push(resultAddr);

            return state.WithStackAndHeap(newStack, newHeap);
        }

        private TiState StepInd(TiState state, TiNode.Indirection indirection)
        {
            var newStack = state.Stack.Replace(indirection.Address);

            return state.WithStack(newStack);
        }

        private TiState StepPrim(TiState state, TiNode.Primitive primitive)
        {
            return primitive.Type switch
            {
                PrimitiveType.Neg => PrimNeg(state),
                PrimitiveType.Add => PrimArith(state, (a, b) => a + b),
                PrimitiveType.Sub => PrimArith(state, (a, b) => a - b),
                PrimitiveType.Mul => PrimArith(state, (a, b) => a * b),
                PrimitiveType.Div => PrimArith(state, (a, b) => a / b),

                _ => throw new ArgumentOutOfRangeException(nameof(primitive))
            };
        }

        private static TiState PrimNeg(TiState state)
        {
            var (newStack, argAddrs, root) = GetArgs(state, 1);

            var argNode = state.Heap[argAddrs[0]];

            if (!IsDataNode(argNode))
            {
                newStack = newStack.Push(argAddrs[0]);
                var stackToDump = ImmutableStack<int>.Empty.Push(root);

                return state.WithStackAndPushDump(newStack, stackToDump);
            }

            var dataNode = (TiNode.Number) argNode;
            var result = new TiNode.Number(-dataNode.Value);
            newStack = newStack.Push(root);
            var newHeap = state.Heap.SetItem(root, result);

            return state.WithStackAndHeap(newStack, newHeap);
        }

        private TiState PrimArith(TiState state, Func<int, int, int> fn)
        {
            var (newStack, argAddrs, root) = GetArgs(state, 2);

            var leftNode = state.Heap[argAddrs[0]];
            var rightNode = state.Heap[argAddrs[1]];

            if (!IsDataNode(leftNode))
            {
                newStack = newStack.Push(argAddrs[0]);
                var stackToDump = ImmutableStack<int>.Empty.Push(root);

                return state.WithStackAndPushDump(newStack, stackToDump);
            }

            if (!IsDataNode(rightNode))
            {
                newStack = newStack.Push(argAddrs[1]);
                var stackToDump = ImmutableStack<int>.Empty.Push(root);

                return state.WithStackAndPushDump(newStack, stackToDump);
            }

            var leftDataNode = (TiNode.Number) leftNode;
            var rightDataNode = (TiNode.Number) rightNode;

            var result = new TiNode.Number(fn(leftDataNode.Value, rightDataNode.Value));
            newStack = newStack.Push(root);
            var newHeap = state.Heap.SetItem(root, result);

            return state.WithStackAndHeap(newStack, newHeap);
        }

        private static (ImmutableStack<int> Stack, IReadOnlyList<int> ArgumentAddresses, int Root) GetArgs(TiState state, int argCount)
        {
            var (newStack, items) = state.Stack.PopMultiple(argCount + 1);

            var args = items.Skip(1).Select(addr => state.Heap[addr]).Cast<TiNode.Application>().Select(node => node.Argument).ToList();
            return (newStack, args, items.Last());
        }

        private static (ImmutableDictionary<int, TiNode>, int) Instantiate(Expression<Name> expr, ImmutableDictionary<int,TiNode> heap, ImmutableDictionary<Name,int> env, int? target)
        {
            switch (expr)
            {
                case Expression<Name>.Number num:
                    return AssignOrAllocate(target, heap, new TiNode.Number(num.Value));

                case Expression<Name>.Application ap:
                    var (heap1, a1) = Instantiate(ap.Function, heap, env, null);
                    var (heap2, a2) = Instantiate(ap.Parameter, heap1, env, null);

                    return AssignOrAllocate(target, heap2, new TiNode.Application(a1, a2));

                case Expression<Name>.Variable variable:
                    if (target.HasValue)
                    {
                        return (Assign(target.Value, heap, new TiNode.Indirection(env[variable.Name])), target.Value);
                    }
                    return (heap, env[variable.Name]);

                case Expression<Name>.Let let when !let.IsRecursive:
                {
                    var defns = new Dictionary<Name, int>();
                    foreach (var definition in let.Definitions)
                    {
                        var (newHeap, newAddr) = Instantiate(definition.Item2, heap, env, null);
                        defns.Add(definition.Item1, newAddr);
                        heap = newHeap;
                    }

                    var newEnv = env.SetItems(defns);

                    return Instantiate(let.Body, heap, newEnv, null);
                }

                case Expression<Name>.Let let when let.IsRecursive:
                {
                    foreach (var (name, _) in let.Definitions)
                    {
                        var (newHeap, placeholder) = Allocate(heap, new TiNode.Indirection(-1));
                        heap = newHeap;
                        env = env.SetItem(name, placeholder);
                    }

                    foreach (var (name, defExpr) in let.Definitions)
                    {
                        var targetAddr = env[name];
                        (heap, _) = Instantiate(defExpr, heap, env, targetAddr);
                    }

                    return Instantiate(let.Body, heap, env, null);
                }

                default: throw new ArgumentOutOfRangeException(nameof(expr));
            }
        }

        private static (ImmutableDictionary<int, TiNode>, int) AssignOrAllocate(int? addr, ImmutableDictionary<int, TiNode> heap, TiNode node)
        {
            return addr.HasValue ? (Assign(addr.Value, heap, node), addr.Value) : Allocate(heap, node);
        }

        private static ImmutableDictionary<int, TiNode> Assign(int addr, ImmutableDictionary<int,TiNode> heap, TiNode node)
        {
            return heap.SetItem(addr, node);
        }
        private static (ImmutableDictionary<int, TiNode>, int) Allocate(ImmutableDictionary<int,TiNode> heap, TiNode node)
        {
            var addr = heap.Keys.Max() + 1;
            var newHeap = heap.Add(addr, node);

            return (newHeap, addr);
        }

        private static bool IsComplete(TiState state)
        {
            if (!state.Dump.IsEmpty)
            {
                return false;
            }

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