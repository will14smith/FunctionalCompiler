using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FuncComp.Parsing;
using FuncComp.TemplateInstantiation;

namespace FuncComp
{
    class Program
    {
        static void Main(string[] args)
        {
            // foreach (var token in Lexer.lex("123 abc a1_1 || this is a comment\n123 == 123\nmain = 5")) Console.WriteLine(token);

            // var progStr = ProgramPrinter.Print(prog);
            // var progStr = "square x = x * x; main = square (square 3)";

            // var progStr = "main = (I 100) + (I 200)";
            // var progStr = "main = Pack{2,2} 2 (Pack{2,2} 1 Pack{1,0})";
            // var progStr = "fac n = if (n == 0) 1 (n * fac (n - 1)); main = fac 10";
            var progStr = "main = fst (snd (fst (MkPair (MkPair 1 (MkPair 2 3)) 4)))";

            var tokens = Lexer.lex(progStr);
            var prog = Parser.parse(tokens);

            // template instantiation
            {
                var compiler = new TiCompiler();
                var initialState = compiler.Compile(prog);

                var evaluator = new TiEvaluator();
                var states = evaluator.Evaluate(initialState).ToList();

                Console.WriteLine($"Took {states.Count} states and {states.Last().Heap.Count} heap entries");
                var finalState = states.Last();
                Console.WriteLine(TiStatePrinter.Print(finalState));

                DrawAllStates(states);
            }
        }

        private static void DrawAllStates(IEnumerable<TiState> states)
        {
            var tmp = Path.GetTempFileName();
            Directory.CreateDirectory("states");

            var exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "bin", "dot.exe");

            var i = 0;
            foreach (var state in states)
            {
                var graph = TiStateGraphPrinter.Print(state);
                File.WriteAllText(tmp, graph);

                var si = new ProcessStartInfo {FileName = exe, Arguments = $"-T png -o \"{Path.Combine("states", $"state{i}.png")}\" \"{tmp}\""};
                var p = new Process {StartInfo = si};
                p.Start();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    Console.WriteLine(graph);
                }

                i++;
            }

            File.Delete(tmp);
        }
    }
}
