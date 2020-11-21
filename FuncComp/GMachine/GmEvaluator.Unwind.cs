using System;
using System.Collections.Generic;

namespace FuncComp.GMachine
{
    public partial class GmEvaluator
    {
        private static GmState StepUnwind(GmInstruction.Unwind _, GmState state)
        {
            var head = state.Stack.Peek();
            var headNode = state.Heap[head];

            return headNode switch
            {
                GmNode.Number num => UnwindNum(num, state),
                GmNode.Application ap => UnwindAp(ap, state),
                GmNode.Global global => UnwindGlobal(global, state),
                GmNode.Indirection ind => UnwindInd(ind, state),

                _ => throw new ArgumentOutOfRangeException(nameof(headNode))
            };
        }

        private static GmState UnwindNum(GmNode.Number num, GmState state)
        {
            if (state.Dump.IsEmpty)
            {
                return state;
            }

            var head = state.Stack.Peek();

            return state.PopDump().Push(head);
        }

        private static GmState UnwindAp(GmNode.Application application, GmState state)
        {
            return state.Push(application.Function).EnqueueInstruction(GmInstruction.Unwind.Instance);
        }

        private static GmState UnwindGlobal(GmNode.Global global, GmState state)
        {
            IReadOnlyList<int> args;
            (state, args) = state.Pop(global.ArgCount + 1);

            state = state.Push(args[global.ArgCount]);

            // args[0] is the Global node
            for (var i = args.Count - 1; i >= 1; i--)
            {
                var apNode = (GmNode.Application) state.Heap[args[i]];
                state = state.Push(apNode.Argument);
            }

            return state.WithCode(global.Code);
        }

        private static GmState UnwindInd(GmNode.Indirection ind, GmState state)
        {
            return state.Drop(1).Push(ind.Addr).EnqueueInstruction(GmInstruction.Unwind.Instance);
        }
    }
}