using System;
using System.Collections.Immutable;
using System.Linq;

namespace FuncComp.TemplateInstantiation
{
    public partial class TiEvaluator
    {
        private TiState StepPrim(TiState state, TiNode.Primitive primitive)
        {
            return primitive.Type switch
            {
                PrimitiveType.Constructor constr => PrimConstr(state, constr),

                PrimitiveType.Neg _ => PrimUnary(state, a => -a),
                PrimitiveType.Add _ => PrimBinaryArith(state, (a, b) => a + b),
                PrimitiveType.Sub _ => PrimBinaryArith(state, (a, b) => a - b),
                PrimitiveType.Mul _ => PrimBinaryArith(state, (a, b) => a * b),
                PrimitiveType.Div _ => PrimBinaryArith(state, (a, b) => a / b),

                PrimitiveType.Greater _ => PrimBinaryComp(state, (a, b) => a > b),
                PrimitiveType.GreaterEqual _ => PrimBinaryComp(state, (a, b) => a >= b),
                PrimitiveType.Less _ => PrimBinaryComp(state, (a, b) => a < b),
                PrimitiveType.LessEqual _ => PrimBinaryComp(state, (a, b) => a <= b),
                PrimitiveType.Equal _ => PrimBinaryComp(state, (a, b) => a == b),
                PrimitiveType.NotEqual _ => PrimBinaryComp(state, (a, b) => a != b),

                PrimitiveType.Abort _ => throw new Exception("aborting"),
                PrimitiveType.If _ => PrimIf(state),
                PrimitiveType.CasePair _ => PrimCasePair(state),
                PrimitiveType.CaseList _ => PrimCaseList(state),
                PrimitiveType.Stop _ => PrimStop(state),
                PrimitiveType.Print _ => PrimPrint(state),

                _ => throw new ArgumentOutOfRangeException(nameof(primitive))
            };
        }

        private TiState PrimConstr(TiState state, PrimitiveType.Constructor constr)
        {
            var (newStack, argAddrs, root) = GetArgs(state, constr.Arity);

            var node = new TiNode.Data(constr.Tag, argAddrs);

            newStack = newStack.Push(root);
            var newHeap = state.Heap.SetItem(root, node);

            return state.WithStackAndHeap(newStack, newHeap);
        }

        private static TiState PrimUnary(TiState state, Func<int, int> fn)
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
            var result = new TiNode.Number(fn(dataNode.Value));
            newStack = newStack.Push(root);
            var newHeap = state.Heap.SetItem(root, result);

            return state.WithStackAndHeap(newStack, newHeap);
        }

        private TiState PrimBinaryArith(TiState state, Func<int, int, int> fn)
        {
            return PrimBinary(state, (a, b) => new TiNode.Number(fn(((TiNode.Number) a).Value, ((TiNode.Number) b).Value)));
        }

        private TiState PrimBinaryComp(TiState state, Func<int, int, bool> fn)
        {
            return PrimBinary(state, (a, b) => new TiNode.Data(fn(((TiNode.Number) a).Value, ((TiNode.Number) b).Value) ? 2 : 1, new int[0]));
        }

        private TiState PrimBinary(TiState state, Func<TiNode, TiNode, TiNode> fn)
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

            var result = fn(leftNode, rightNode);
            newStack = newStack.Push(root);
            var newHeap = state.Heap.SetItem(root, result);

            return state.WithStackAndHeap(newStack, newHeap);
        }

        private TiState PrimIf(TiState state)
        {
            var (newStack, argAddrs, root) = GetArgs(state, 3);

            var conditionNode = state.Heap[argAddrs[0]];

            if (!IsDataNode(conditionNode))
            {
                newStack = newStack.Push(argAddrs[0]);
                var stackToDump = ImmutableStack<int>.Empty.Push(root);

                return state.WithStackAndPushDump(newStack, stackToDump);
            }

            var conditionDataNode = (TiNode.Data) conditionNode;

            if (conditionDataNode.Tag < 1 || conditionDataNode.Tag > 2 || conditionDataNode.Components.Any())
            {
                throw new InvalidOperationException("not a boolean");
            }

            var result = new TiNode.Indirection(conditionDataNode.Tag == 2 ? argAddrs[1] : argAddrs[2]);

            newStack = newStack.Push(root);
            var newHeap = state.Heap.SetItem(root, result);

            return state.WithStackAndHeap(newStack, newHeap);
        }

        private TiState PrimCasePair(TiState state)
        {
            var (newStack, argAddrs, root) = GetArgs(state, 2);

            var pairNode = state.Heap[argAddrs[0]];

            if (!IsDataNode(pairNode))
            {
                newStack = newStack.Push(argAddrs[0]);
                var stackToDump = ImmutableStack<int>.Empty.Push(root);

                return state.WithStackAndPushDump(newStack, stackToDump);
            }

            var pairDataNode = (TiNode.Data) pairNode;

            if (pairDataNode.Tag != 1 || pairDataNode.Components.Count != 2)
            {
                throw new InvalidOperationException("not a pair");
            }

            // result = Ap (Ap f a) b
            var ap = new TiNode.Application(argAddrs[1], pairDataNode.Components[0]);
            var (newHeap, apAddr) = Allocate(state.Heap, ap);

            var result = new TiNode.Application(apAddr, pairDataNode.Components[1]);

            newStack = newStack.Push(root);
            newHeap = newHeap.SetItem(root, result);

            return state.WithStackAndHeap(newStack, newHeap);
        }

        private TiState PrimCaseList(TiState state)
        {
            var (newStack, argAddrs, root) = GetArgs(state, 3);

            var listNode = state.Heap[argAddrs[0]];

            if (!IsDataNode(listNode))
            {
                newStack = newStack.Push(argAddrs[0]);
                var stackToDump = ImmutableStack<int>.Empty.Push(root);

                return state.WithStackAndPushDump(newStack, stackToDump);
            }

            var listDataNode = (TiNode.Data) listNode;


            if (listDataNode.Tag < 1 || listDataNode.Tag > 2 || (listDataNode.Tag == 1 && listDataNode.Components.Any()) || (listDataNode.Tag == 2 && listDataNode.Components.Count != 2))
            {
                throw new InvalidOperationException("not a list");
            }

            TiNode result;
            var newHeap = state.Heap;

            if (listDataNode.Tag == 1)
            {
                result = new TiNode.Indirection(argAddrs[1]);
            }
            else
            {
                // result = Ap (Ap f a) b
                var ap = new TiNode.Application(argAddrs[2], listDataNode.Components[0]);
                int apAddr;
                (newHeap, apAddr) = Allocate(state.Heap, ap);

                result = new TiNode.Application(apAddr, listDataNode.Components[1]);
            }

            newStack = newStack.Push(root);
            newHeap = newHeap.SetItem(root, result);

            return state.WithStackAndHeap(newStack, newHeap);
        }

        private TiState PrimStop(TiState state)
        {
            var (newStack, _, _) = GetArgs(state, 0);

            if (!newStack.IsEmpty)
            {
                throw new NotImplementedException("invalid stack to stop on");
            }

            if (!state.Dump.IsEmpty)
            {
                throw new NotImplementedException("invalid dump to stop on");
            }

            return state.WithStack(ImmutableStack<int>.Empty);
        }

        private TiState PrimPrint(TiState state)
        {
            var (newStack, argAddrs, root) = GetArgs(state, 2);

            var valueNode = state.Heap[argAddrs[0]];

            if (!IsDataNode(valueNode))
            {
                newStack = newStack.Push(argAddrs[0]);
                var stackToDump = ImmutableStack<int>.Empty.Push(root);

                return state.WithStackAndPushDump(newStack, stackToDump);
            }

            if (!state.Dump.IsEmpty)
            {
                throw new NotImplementedException("invalid dump to print with");
            }

            var valueDataNode = (TiNode.Number) valueNode;

            newStack = newStack.Push(argAddrs[1]);

            return state.WithStackAndAppendOutput(newStack, valueDataNode.Value);
        }
    }
}