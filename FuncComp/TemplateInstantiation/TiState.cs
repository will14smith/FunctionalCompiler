using System.Collections.Immutable;
using FuncComp.Language;

namespace FuncComp.TemplateInstantiation
{
    public class TiState
    {
        public TiState(ImmutableStack<int> stack, ImmutableDictionary<int, TiNode> heap, ImmutableDictionary<Name, int> globals)
        {
            Stack = stack;
            Heap = heap;
            Globals = globals;
        }

        public ImmutableStack<int> Stack { get; }
        // Dump
        public ImmutableDictionary<int, TiNode> Heap { get; }
        public ImmutableDictionary<Name, int> Globals { get; }

        public TiState PushStack(in int addr)
        {
            return new TiState(Stack.Push(addr), Heap, Globals);
        }

        public TiState WithStackAndHeap(ImmutableStack<int> newStack, ImmutableDictionary<int, TiNode> newHeap)
        {
            return new TiState(newStack, newHeap, Globals);
        }
    }
}