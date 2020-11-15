using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FuncComp.Helpers;
using static FuncComp.Helpers.PrettyPrinter.NodeBuilder;

namespace FuncComp.TemplateInstantiation
{
    public static class TiStateGraphPrinter
    {
        public static string Print(TiState state)
        {
            var node = ShowState(state);

            return PrettyPrinter.Display(node);
        }

        private static PrettyPrinter.Node ShowState(TiState state)
        {
            var (stack, usages1) = ShowStack(state.Stack);
            var (dump, usages2) = ShowDump(state.Dump);
            var (heap, usages3) = ShowHeap(state.Heap);

            var usages = usages1.Union(usages2).Union(usages3).ToHashSet();

            var visibleHeap = Append(heap.Where(x => usages.Contains(x.Key)).Select(x => x.Value).ToArray());

            return Append(
                Str("digraph {"),
                Newline(),
                Str("  "),
                Indent(Append(
                    Str("node [shape=record style=filled];"),
                    Newline(),
                    stack,
                    dump,
                    visibleHeap
                )),
                Newline(),
                Str("}"));
        }

        private static (PrettyPrinter.Node, IEnumerable<int>) ShowStack(ImmutableStack<int> stack)
        {
            var i = 0;
            var node = Str("stack [label=\"");
            var heap = Nil();
            var usages = new HashSet<int>();

            while (!stack.IsEmpty)
            {
                stack = stack.Pop(out var addr);

                if (i > 0)
                {
                    node = Append(node, Str("|"));
                }

                node = Append(node, Str($"<s{i}> {addr}"));
                heap = Append(heap, Str($"stack:s{i} -> heap{addr};"), Newline());
                usages.Add(addr);

                i++;
            }

            node = Append(node, Str("\" fillcolor=white];"));

            var result = Append(node, Newline(), heap, Newline());
            return (result, usages);
        }

        private static (PrettyPrinter.Node, IEnumerable<int>) ShowDump(ImmutableStack<ImmutableStack<int>> dump)
        {
            var j = 0;

            var result = Nil();
            var usages = new HashSet<int>();

            while (!dump.IsEmpty)
            {
                dump = dump.Pop(out var stack);

                var i = 0;
                var node = Str($"dump{j} [label=\"");
                var heap = Nil();

                while (!stack.IsEmpty)
                {
                    stack = stack.Pop(out var addr);

                    if (i > 0)
                    {
                        node = Append(node, Str("|"));
                    }

                    node = Append(node, Str($"<s{i}> {addr}"));
                    heap = Append(heap, Str($"dump{j}:s{i} -> heap{addr};"), Newline());
                    usages.Add(addr);

                    i++;
                }

                node = Append(node, Str("\" fillcolor=blanchedalmond];"));
                result = Append(result, node, Newline(), heap, Newline());

                j++;
            }

            return (result, usages);
        }

        private static (IReadOnlyDictionary<int, PrettyPrinter.Node>, IEnumerable<int>) ShowHeap(ImmutableDictionary<int,TiNode> heap)
        {
            var results = new Dictionary<int, PrettyPrinter.Node>();
            var usages = new HashSet<int>();

            foreach (var (addr, node) in heap)
            {
                var result = Str($"heap{addr}");

                switch (node)
                {
                    case TiNode.Application application:
                        result = Append(result, Str($"[label=\"{addr}: Ap {application.Function} {application.Argument}\"];"), Newline());
                        result = Append(result, Str($"heap{addr} -> heap{application.Function};"), Newline());
                        result = Append(result, Str($"heap{addr} -> heap{application.Argument} [style=dashed]"));

                        usages.Add(application.Function);
                        usages.Add(application.Argument);

                        break;
                    case TiNode.Indirection indirection:
                        result = Append(result, Str($"[label=\"{addr}: Ind {indirection.Address}\"];"), Newline());
                        result = Append(result, Str($"heap{addr} -> heap{indirection.Address}"));

                        usages.Add(indirection.Address);

                        break;
                    case TiNode.Number number:
                        result = Append(result, Str($"[label=\"{addr}: Num {number.Value}\"]"));
                        break;
                    case TiNode.Primitive primitive:
                        result = Append(result, Str($"[label=\"{addr}: Prim {primitive.Name}\"]"));
                        break;
                    case TiNode.Supercombinator supercombinator:
                        result = Append(result, Str($"[label=\"{addr}: Sc {supercombinator.Name}\"]"));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(node));
                }

                results.Add(addr, Append(result, Str(";"), Newline()));
            }

            return (results, usages);
        }
    }
}