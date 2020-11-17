using System;
using System.Linq;
using FuncComp.Helpers;
using static FuncComp.Helpers.PrettyPrinter.NodeBuilder;

namespace FuncComp.GMachine
{
    public static class GmStatePrinter
    {
        public static string Print(GmState state) => PrettyPrinter.Display(ShowState(state));

        private static PrettyPrinter.Node ShowState(GmState state) => Append(ShowStack(state), Newline());

        private static PrettyPrinter.Node ShowStack(GmState state)  =>
            Append(
                Str("Stk ["),
                Indent(Interleave(new PrettyPrinter.Node.Newline(), state.Stack.Select(addr => Append(
                    ShowAddr(addr),
                    Str(": "),
                    ShowNode(state, addr)
                )))),
                Str(" ]")
            );

        private static PrettyPrinter.Node ShowNode(GmState state, int addr) =>
            state.Heap[addr] switch
            {
                GmNode.Number number => Str($"Num {number.Value}"),
                GmNode.Application application => Str($"Ap {application.Function} {application.Argument}"),
                GmNode.Global _ => Str($"Global {state.Globals.Single(x => x.Value == addr).Key}"),

                _ => throw new ArgumentOutOfRangeException(nameof(addr))
            };


        private static PrettyPrinter.Node ShowAddr(int addr) => Str(addr.ToString("0000"));
    }
}