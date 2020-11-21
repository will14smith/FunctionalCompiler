using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FuncComp.Helpers;

namespace FuncComp.GMachine
{
    public partial class GmEvaluator
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
                GmInstruction.Slide slide => StepSlide(slide, newState),
                GmInstruction.Alloc alloc => StepAlloc(alloc, newState),
                GmInstruction.Unwind unwind => StepUnwind(unwind, newState),
                GmInstruction.Eval eval => StepEval(eval, newState),
                GmInstruction.Prim prim => StepPrim(prim, newState),
                GmInstruction.Cond cond => StepCond(cond, newState),

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
            var arg = state.Stack.GetNth(instruction.Offset);

            return state.Push(arg);
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

        private static GmState StepSlide(GmInstruction.Slide instruction, GmState state)
        {
            var head = state.Stack.Peek();

            state = state.Drop(instruction.Count + 1);

            return state.Push(head);
        }

        private static GmState StepAlloc(GmInstruction.Alloc instruction, GmState state)
        {
            var count = instruction.Count;

            while (count-- > 0)
            {
                state = state.AllocateAndPush(new GmNode.Indirection(-1));
            }

            return state;
        }

        private static GmState StepEval(GmInstruction.Eval _, GmState state)
        {
            int head;
            (state, head) = state.Pop();

            var newCode = ImmutableQueue<GmInstruction>.Empty.Enqueue(GmInstruction.Unwind.Instance);
            var newStack = ImmutableStack<int>.Empty.Push(head);

            return state.PushDump(newCode, newStack);
        }

        private static GmState StepCond(GmInstruction.Cond instruction, GmState state)
        {
            int head;
            (state, head) = state.Pop();

            var node = (GmNode.Number)state.Heap[head];

            var code = node.Value switch
            {
                1 => instruction.TrueCode,
                0 => instruction.FalseCode,

                _ => throw new InvalidOperationException("invalid boolean value"),
            };

            return state.WithCode(code.EnqueueRange(state.Code));
        }
    }
}