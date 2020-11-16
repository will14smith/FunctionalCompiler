using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FuncComp.Helpers;

namespace FuncComp.TemplateInstantiation
{
    public partial class TiEvaluator
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
                TiNode.Data data => StepData(state, data),

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

        private TiState StepData(TiState state, TiNode.Data data)
        {
            var stackOnlyHasData = state.Stack.Pop().IsEmpty;
            var dumpIsEmpty = state.Dump.IsEmpty;

            if (!stackOnlyHasData || dumpIsEmpty)
            {
                throw new Exception("Data applied as function");
            }

            return state.PopDump();
        }

        private static (ImmutableStack<int> Stack, IReadOnlyList<int> ArgumentAddresses, int Root) GetArgs(TiState state, int argCount)
        {
            var (newStack, items) = state.Stack.PopMultiple(argCount + 1);

            var args = items.Skip(1).Select(addr => state.Heap[addr]).Cast<TiNode.Application>().Select(node => node.Argument).ToList();
            return (newStack, args, items.Last());
        }

        private static bool IsComplete(TiState state)
        {
            if (!state.Dump.IsEmpty)
            {
                return false;
            }

            if (state.Stack.IsEmpty)
            {
                return true;
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
            return node is TiNode.Number || node is TiNode.Data;
        }
    }
}