using System.Collections.Immutable;
using FuncComp.Language;

namespace FuncComp.TemplateInstantiation
{
    public class TiState
    {
        public TiState(ImmutableStack<int> stack, ImmutableStack<ImmutableStack<int>> dump, ImmutableDictionary<int, TiNode> heap, ImmutableDictionary<Name, int> globals)
        {
            Stack = stack;
            Dump = dump;
            Heap = heap;
            Globals = globals;
        }

        public ImmutableStack<int> Stack { get; }
        public ImmutableStack<ImmutableStack<int>> Dump { get; }
        public ImmutableDictionary<int, TiNode> Heap { get; }
        public ImmutableDictionary<Name, int> Globals { get; }

        public TiState PushStack(in int addr)
        {
            return WithStack(Stack.Push(addr));
        }

        public TiState WithStack(ImmutableStack<int> newStack)
        {
            return new TiState(newStack, Dump, Heap, Globals);
        }
        public TiState WithStackAndPushDump(ImmutableStack<int> newStack, ImmutableStack<int> stackToDump)
        {
            return new TiState(newStack, Dump.Push(stackToDump), Heap, Globals);
        }
        public TiState WithStackAndHeap(ImmutableStack<int> newStack, ImmutableDictionary<int, TiNode> newHeap)
        {
            return new TiState(newStack, Dump, newHeap, Globals);
        }

        public TiState WithHeap(ImmutableDictionary<int, TiNode> newHeap)
        {
            return new TiState(Stack, Dump, newHeap, Globals);
        }

        public TiState PopDump()
        {
            var newDump = Dump.Pop(out var newStack);

            return new TiState(newStack, newDump, Heap, Globals);
        }
    }
}