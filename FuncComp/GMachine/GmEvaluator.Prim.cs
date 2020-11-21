using System;
using System.Collections.Generic;

namespace FuncComp.GMachine
{
    public partial class GmEvaluator
    {
        private static readonly IReadOnlyDictionary<GmInstruction.Prim.PrimType, Func<GmState, GmState>> PrimHandlers = new Dictionary<GmInstruction.Prim.PrimType,Func<GmState,GmState>>
        {
            { GmInstruction.Prim.PrimType.Neg, Arithmetic1(a => -a) },
            { GmInstruction.Prim.PrimType.Add, Arithmetic2((a, b) => a + b) },
            { GmInstruction.Prim.PrimType.Sub, Arithmetic2((a, b) => a - b) },
            { GmInstruction.Prim.PrimType.Mul, Arithmetic2((a, b) => a * b) },
            { GmInstruction.Prim.PrimType.Div, Arithmetic2((a, b) => a / b) },

            { GmInstruction.Prim.PrimType.Eq, Comparison((a, b) => a == b) },
            { GmInstruction.Prim.PrimType.Ne, Comparison((a, b) => a != b) },
            { GmInstruction.Prim.PrimType.Lt, Comparison((a, b) => a < b) },
            { GmInstruction.Prim.PrimType.Le, Comparison((a, b) => a <= b) },
            { GmInstruction.Prim.PrimType.Gt, Comparison((a, b) => a > b) },
            { GmInstruction.Prim.PrimType.Ge, Comparison((a, b) => a >= b) },
        };

        private static GmState StepPrim(GmInstruction.Prim instruction, GmState state)
        {
            if (!PrimHandlers.TryGetValue(instruction.Type, out var handler))
            {
                throw new KeyNotFoundException(instruction.Type.ToString());
            }

            return handler(state);
        }

        private static GmState BoxNum(int value, GmState state)
        {
            return state.AllocateAndPush(new GmNode.Number(value));
        }
        private static int UnboxNum(int addr, GmState state)
        {
            return ((GmNode.Number) state.Heap[addr]).Value;
        }

        private static GmState BoxBool(bool value, GmState state)
        {
            return state.AllocateAndPush(new GmNode.Number(value ? 1 : 0));
        }

        private static Func<GmState, GmState> Primitive1<TA, TB>(Func<TB, GmState, GmState> box, Func<int, GmState, TA> unbox, Func<TA, TB> op) =>
            state =>
            {
                var (newState, head) = state.Pop();

                var result = op(unbox(head, state));

                return box(result, newState);
            };

        private static Func<GmState, GmState> Primitive2<TA, TB>(Func<TB, GmState, GmState> box, Func<int, GmState, TA> unbox, Func<TA, TA, TB> op) =>
            state =>
            {
                int head1;

                var (newState, head0) = state.Pop();
                (newState, head1) = newState.Pop();

                var unbox0 = unbox(head0, state);
                var unbox1 = unbox(head1, state);
                var result = op(unbox0, unbox1);

                return box(result, newState);
            };

        private static Func<GmState, GmState> Arithmetic1(Func<int, int> op) => state => Primitive1(BoxNum, UnboxNum, op)(state);
        private static Func<GmState, GmState> Arithmetic2(Func<int, int, int> op) => state => Primitive2(BoxNum, UnboxNum, op)(state);
        private static Func<GmState, GmState> Comparison(Func<int, int, bool> op) => state => Primitive2(BoxBool, UnboxNum, op)(state);
    }
}