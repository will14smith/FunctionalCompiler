using System;
using System.Collections.Generic;
using FuncComp.Helpers;

namespace FuncComp.GMachine
{
    public class GmEvaluator
    {
        public IEnumerable<GmState> Evaluate(GmState state)
        {
            yield return state;

            while (!IsComplete(state))
            {
                state = Step(state);

                yield return state;
            }
        }

        private static bool IsComplete(GmState state)
        {
            return state.Code.IsEmpty;
        }

        private static GmState Step(GmState state)
        {
            var (instruction, newState) = state.DequeueInstruction();

            return instruction switch
            {
                GmInstruction.PushGlobal pushGlobal => StepPushGlobal(pushGlobal, newState),
                GmInstruction.PushInt pushInt => StepPushInt(pushInt, newState),
                GmInstruction.MkAp mkAp => StepMkAp(mkAp, newState),
                GmInstruction.Push push => StepPush(push, newState),
                GmInstruction.Update update => StepUpdate(update, newState),
                GmInstruction.Pop pop => StepPop(pop, newState),
                GmInstruction.Unwind unwind => StepUnwind(unwind, newState),

                _ => throw new ArgumentOutOfRangeException(nameof(instruction))
            };
        }

        private static GmState StepPushGlobal(GmInstruction.PushGlobal instruction, GmState state)
        {
            if (!state.Globals.TryGetValue(instruction.Name, out var addr))
            {
                throw new KeyNotFoundException(instruction.Name.Value);
            }

            return state.Push(addr);
        }

        private static GmState StepPushInt(GmInstruction.PushInt instruction, GmState state)
        {
            var node = new GmNode.Number(instruction.Value);
            return state.AllocateAndPush(node);
        }

        private static GmState StepMkAp(GmInstruction.MkAp _, GmState state)
        {
            int function, argument;
            (state, function) = state.Pop();
            (state, argument) = state.Pop();

            int apAddr;
            (state, apAddr) = state.Allocate(new GmNode.Application(function, argument));

            return state.Push(apAddr);
        }

        private static GmState StepPush(GmInstruction.Push instruction, GmState state)
        {
            var apAddr = state.Stack.GetNth(instruction.Offset + 1);

            var apNode = (GmNode.Application)state.Heap[apAddr];
            var argument = apNode.Argument;

            return state.Push(argument);
        }

        private static GmState StepUpdate(GmInstruction.Update instruction, GmState state)
        {
            int head;
            (state, head) = state.Pop();
            var apAddr = state.Stack.GetNth(instruction.Offset);

            var node = new GmNode.Indirection(head);
            return state.Set(apAddr, node);
        }

        private static GmState StepPop(GmInstruction.Pop instruction, GmState state)
        {
            return state.Drop(instruction.Count);
        }

        private static GmState StepUnwind(GmInstruction.Unwind _, GmState state)
        {
            var head = state.Stack.Peek();
            var headNode = state.Heap[head];

            return headNode switch
            {
                GmNode.Number _ => state,
                GmNode.Application ap => UnwindAp(ap, state),
                GmNode.Global global => UnwindGlobal(global, state),
                GmNode.Indirection ind => UnwindInd(ind, state),

                _ => throw new ArgumentOutOfRangeException(nameof(headNode))
            };
        }

        private static GmState UnwindAp(GmNode.Application application, GmState state)
        {
            return state.Push(application.Function).EnqueueInstruction(GmInstruction.Unwind.Instance);
        }

        private static GmState UnwindGlobal(GmNode.Global global, GmState state)
        {
            // TODO check stack size
            return state.WithCode(global.Code);
        }

        private static GmState UnwindInd(GmNode.Indirection ind, GmState state)
        {
            return state.Drop(1).Push(ind.Addr).EnqueueInstruction(GmInstruction.Unwind.Instance);
        }
    }
}