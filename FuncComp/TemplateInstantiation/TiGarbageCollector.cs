using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FuncComp.Language;

namespace FuncComp.TemplateInstantiation
{
    public class TiGarbageCollector
    {
        public static TiState Collect(TiState state)
        {
            ImmutableStack<int> newStack;
            ImmutableStack<ImmutableStack<int>> newDump;
            ImmutableDictionary<Name, int> newGlobals;

            var newHeap = state.Heap;
            (newHeap, newStack) = MarkFromStack(newHeap, state.Stack);
            (newHeap, newDump) = MarkFromDump(newHeap, state.Dump);
            (newHeap, newGlobals) = MarkFromGlobals(newHeap, state.Globals);
            newHeap = ScanHeap(newHeap);

            return new TiState(state.Output, newStack, newDump, newHeap, newGlobals);
        }

        private static (ImmutableDictionary<int, TiNode>, ImmutableStack<int>) MarkFromStack(ImmutableDictionary<int, TiNode> heap, ImmutableStack<int> stack)
        {
            var newStack = ImmutableStack<int>.Empty;

            foreach(var addr in stack.Reverse())
            {
                int newAddr;
                (heap, newAddr) = MarkFrom(heap, addr);
                newStack = newStack.Push(newAddr);
            }

            return (heap, newStack);
        }
        private static (ImmutableDictionary<int, TiNode>, ImmutableStack<ImmutableStack<int>>) MarkFromDump(ImmutableDictionary<int, TiNode> heap, ImmutableStack<ImmutableStack<int>> dump)
        {
            var newDump = ImmutableStack<ImmutableStack<int>>.Empty;

            foreach(var stack in dump.Reverse())
            {
                ImmutableStack<int> newStack;
                (heap, newStack) = MarkFromStack(heap, stack);
                newDump = newDump.Push(newStack);
            }

            return (heap, newDump);
        }
        private static (ImmutableDictionary<int, TiNode>, ImmutableDictionary<Name,int>) MarkFromGlobals(ImmutableDictionary<int, TiNode> heap, ImmutableDictionary<Name,int> globals)
        {
            foreach (var (name, addr) in globals)
            {
                int newAddr;
                (heap, newAddr) = MarkFrom(heap, addr);
                globals = globals.SetItem(name, newAddr);
            }

            return (heap, globals);
        }


        private static (ImmutableDictionary<int, TiNode>, int) MarkFrom(ImmutableDictionary<int, TiNode> heap, int addr)
        {
            var node = heap[addr];

            switch (node)
            {
                case TiNode.Application application:
                    int function, argument;
                    (heap, function) = MarkFrom(heap, application.Function);
                    (heap, argument) = MarkFrom(heap, application.Argument);

                    node = new TiNode.Application(function, argument);
                    break;

                case TiNode.Data data:
                    var components = new List<int>();
                    foreach (var component in data.Components)
                    {
                        int newComponent;
                        (heap, newComponent) = MarkFrom(heap, component);
                        components.Add(newComponent);
                    }

                    node = new TiNode.Data(data.Tag, components);
                    break;

                case TiNode.Indirection indirection: return MarkFrom(heap, indirection.Address);

                case TiNode.Number _: break;
                case TiNode.Primitive _: break;
                case TiNode.Supercombinator _: break;

                case Marked _: return (heap, addr);

                default: throw new ArgumentOutOfRangeException(nameof(node));
            }

            heap = heap.SetItem(addr, new Marked(node));
            return (heap, addr);
        }

        private static ImmutableDictionary<int, TiNode> ScanHeap(ImmutableDictionary<int, TiNode> heap)
        {
            foreach (var (addr, node) in heap)
            {
                if (node is Marked marked)
                {
                    heap = heap.SetItem(addr, marked.Node);
                }
                else
                {
                    heap = heap.Remove(addr);
                }
            }

            return heap;
        }

        private class Marked : TiNode
        {
            public Marked(TiNode node)
            {
                Node = node;
            }

            public TiNode Node { get; }
        }
    }
}