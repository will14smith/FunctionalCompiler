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
        public GmState(ImmutableQueue<GmInstruction> code, ImmutableStack<int> stack, ImmutableDictionary<int, GmNode> heap, ImmutableDictionary<Name, int> globals)
        {
            Code = code;
            Stack = stack;
            Heap = heap;
            Globals = globals;
        }

        public ImmutableQueue<GmInstruction> Code { get; }
        public ImmutableStack<int> Stack { get; }
        public ImmutableDictionary<int, GmNode> Heap { get; }
        public ImmutableDictionary<Name, int> Globals { get; }

        // code
        [Pure]
        public (GmInstruction, GmState) DequeueInstruction()
        {
            var newCode = Code.Dequeue(out var instruction);
            var newState = new GmState(newCode, Stack, Heap, Globals);

            return (instruction, newState);
        }

        [Pure]
        public GmState EnqueueInstruction(GmInstruction instruction)
        {
            var newCode = Code.Enqueue(instruction);

            return new GmState(newCode, Stack, Heap, Globals);
        }

        [Pure]
        public GmState WithCode(ImmutableQueue<GmInstruction> code)
        {
            return new GmState(code, Stack, Heap, Globals);
        }

        // stack
        [Pure]
        public GmState Push(in int value)
        {
            var newStack = Stack.Push(value);

            return new GmState(Code, newStack, Heap, Globals);
        }

        [Pure]
        public (GmState State, int Item) Pop()
        {
            var newStack = Stack.Pop(out var value);
            var newState = new GmState(Code, newStack, Heap, Globals);

            return (newState, value);
        }
        [Pure]
        public (GmState State, IReadOnlyList<int> Items) Pop(in int count)
        {
            var (newStack, items) = Stack.PopMultiple(count);
            var newState = new GmState(Code, newStack, Heap, Globals);

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

            return new GmState(Code, newStack, Heap, Globals);
        }

        // heap
        [Pure]
        public GmState Set(in int addr, GmNode node)
        {
            var newHeap = Heap.SetItem(addr, node);

            return new GmState(Code, Stack, newHeap, Globals);
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