using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using FuncComp.Helpers;
using FuncComp.Language;

namespace FuncComp.GMachine
{
    public class GmState
    {
        public GmState(ImmutableQueue<GmInstruction> code, ImmutableStack<int> stack, ImmutableStack<(ImmutableQueue<GmInstruction> Code, ImmutableStack<int> Stack)> dump, ImmutableDictionary<int, GmNode> heap, ImmutableDictionary<Name, int> globals)
        {
            Code = code;
            Stack = stack;
            Dump = dump;
            Heap = heap;
            Globals = globals;
        }

        public ImmutableQueue<GmInstruction> Code { get; }
        public ImmutableStack<int> Stack { get; }
        public ImmutableStack<(ImmutableQueue<GmInstruction> Code, ImmutableStack<int> Stack)> Dump { get; }
        public ImmutableDictionary<int, GmNode> Heap { get; }
        public ImmutableDictionary<Name, int> Globals { get; }

        // code
        [Pure]
        public (GmInstruction, GmState) DequeueInstruction()
        {
            var newCode = Code.Dequeue(out var instruction);
            var newState = new GmState(newCode, Stack, Dump, Heap, Globals);

            return (instruction, newState);
        }

        [Pure]
        public GmState EnqueueInstruction(GmInstruction instruction)
        {
            var newCode = Code.Enqueue(instruction);

            return new GmState(newCode, Stack, Dump, Heap, Globals);
        }

        [Pure]
        public GmState WithCode(ImmutableQueue<GmInstruction> code)
        {
            return new GmState(code, Stack, Dump, Heap, Globals);
        }

        // stack
        [Pure]
        public GmState Push(in int value)
        {
            var newStack = Stack.Push(value);

            return new GmState(Code, newStack, Dump, Heap, Globals);
        }

        [Pure]
        public (GmState State, int Item) Pop()
        {
            var newStack = Stack.Pop(out var value);
            var newState = new GmState(Code, newStack, Dump, Heap, Globals);

            return (newState, value);
        }
        [Pure]
        public (GmState State, IReadOnlyList<int> Items) Pop(in int count)
        {
            var (newStack, items) = Stack.PopMultiple(count);
            var newState = new GmState(Code, newStack, Dump, Heap, Globals);

            return (newState, items);
        }

        [Pure]
        public GmState Drop(int count)
        {
            var newStack = Stack;

            while (count-- > 0)
            {
                newStack = newStack.Pop();
            }

            return new GmState(Code, newStack, Dump, Heap, Globals);
        }

        // dump
        [Pure]
        public GmState PushDump(ImmutableQueue<GmInstruction> newCode, ImmutableStack<int> newStack)
        {
            var newDump = Dump.Push((Code, Stack));

            return new GmState(newCode, newStack, newDump, Heap, Globals);
        }

        [Pure]
        public GmState PopDump()
        {
            var newDump = Dump.Pop(out var newCodeStack);

            return new GmState(newCodeStack.Code, newCodeStack.Stack, newDump, Heap, Globals);
        }

        // heap
        [Pure]
        public GmState Set(in int addr, GmNode node)
        {
            var newHeap = Heap.SetItem(addr, node);

            return new GmState(Code, Stack, Dump, newHeap, Globals);
        }

        [Pure]
        public (GmState, int) Allocate(GmNode node)
        {
            var addr = Heap.Keys.Max() + 1;
            var newState = Set(addr, node);

            return (newState, addr);
        }

        // helpers
        [Pure]
        public GmState AllocateAndPush(GmNode node)
        {
            var (state, addr) = Allocate(node);
            return state.Push(addr);
        }
    }
}