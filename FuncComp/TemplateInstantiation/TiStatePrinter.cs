using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FuncComp.Helpers;
using static FuncComp.Helpers.PrettyPrinter.NodeBuilder;

namespace FuncComp.TemplateInstantiation
{
    public static class TiStatePrinter
    {
        public static string Print(IEnumerable<TiState> states)
        {
            var node = Append(states.Select(ShowState).ToArray());

            return PrettyPrinter.Display(node);
        }

        public static string Print(TiState state)
        {
            var node = ShowState(state);

            return PrettyPrinter.Display(node);
        }

        private static PrettyPrinter.Node ShowState(TiState state) => Append(ShowOutput(state.Output), Newline(), ShowStack(state.Heap, state.Stack), Newline());

        private static PrettyPrinter.Node ShowOutput(ImmutableList<int> output)
        {
            return Append(
                Str("Out ["),
                Indent(Interleave(new PrettyPrinter.Node.Newline(), output.Select(Num))),
                Str(" ]")
            );
        }

        private static PrettyPrinter.Node ShowStack(ImmutableDictionary<int, TiNode> heap, ImmutableStack<int> stack) =>
            Append(
                Str("Stk ["),
                Indent(Interleave(new PrettyPrinter.Node.Newline(), stack.Select(addr => Append(
                    ShowAddr(addr),
                    Str(": "),
                    ShowStackNode(heap, heap[addr])
                )))),
                Str(" ]")
            );
        
        private static PrettyPrinter.Node ShowStackNode(ImmutableDictionary<int,TiNode> heap, TiNode node) =>
            node switch
            {
                TiNode.Application ap => Append(Str("Ap "), ShowAddr(ap.Function), Str(" "), ShowAddr(ap.Argument), Str(" ("), ShowNode(heap[ap.Argument]), Str(")")),
                _ => ShowNode(node)
            };

        private static PrettyPrinter.Node ShowNode(TiNode node) =>
            node switch
            {
                TiNode.Number number => Str($"Num {number.Value}"),
                TiNode.Application application => Str($"Ap {application.Function} {application.Argument}"),
                TiNode.Supercombinator supercombinator => Str($"Supercombinator {supercombinator.Name}"),
                TiNode.Indirection indirection => Str($"Ind {indirection.Address}"),
                TiNode.Primitive primitive => Str($"Prim {primitive.Name} {primitive.Type}"),
                TiNode.Data data => Append(Str($"Data {data.Tag} ["), Interleave(Str(", "), data.Components.Select(Num)), Str("]")),

                _ => throw new ArgumentOutOfRangeException(nameof(node))
            };

        private static PrettyPrinter.Node ShowAddr(int addr) => Str(addr.ToString("0000"));
    }
}