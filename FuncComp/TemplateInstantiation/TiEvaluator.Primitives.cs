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
                PrimitiveType.Neg _ => PrimUnary(state, a => -a),
                PrimitiveType.Add _ => PrimBinaryArith(state, (a, b) => a + b),
                PrimitiveType.Sub _ => PrimBinaryArith(state, (a, b) => a - b),
                PrimitiveType.Mul _ => PrimBinaryArith(state, (a, b) => a * b),
                PrimitiveType.Div _ => PrimBinaryArith(state, (a, b) => a / b),

                PrimitiveType.If _ => PrimIf(state),

                PrimitiveType.Greater _ => PrimBinaryComp(state, (a, b) => a > b),
                PrimitiveType.GreaterEqual _ => PrimBinaryComp(state, (a, b) => a >= b),
                PrimitiveType.Less _ => PrimBinaryComp(state, (a, b) => a < b),
                PrimitiveType.LessEqual _ => PrimBinaryComp(state, (a, b) => a <= b),
                PrimitiveType.Equal _ => PrimBinaryComp(state, (a, b) => a == b),
                PrimitiveType.NotEqual _ => PrimBinaryComp(state, (a, b) => a != b),

                PrimitiveType.Constructor constr => PrimConstr(state, constr),

                _ => throw new ArgumentOutOfRangeException(nameof(primitive))
            };
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

        private TiState PrimConstr(TiState state, PrimitiveType.Constructor constr)
        {
            var (newStack, argAddrs, root) = GetArgs(state, constr.Arity);

            var node = new TiNode.Data(constr.Tag, argAddrs);

            newStack = newStack.Push(root);
            var newHeap = state.Heap.SetItem(root, node);

            return state.WithStackAndHeap(newStack, newHeap);
        }
    }
}