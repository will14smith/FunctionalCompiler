using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FuncComp.GMachine;
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

            // var progStr = "main = 100";
            // var progStr = "main = I 100";
            // var progStr = "main = K 100 200";
            // var progStr = "main = S K K 3";
            var progStr = "main = twice twice I 3";
            // var progStr = "main = (I 100) + (I 200)";
            // var progStr = "main = Pack{2,2} 2 (Pack{2,2} 1 Pack{1,0})";
            // var progStr = "fac n = if (n == 0) 1 (n * fac (n - 1)); main = fac 10";
            // var progStr = "main = fst (snd (fst (MkPair (MkPair 1 (MkPair 2 3)) 4)))";
            // var progStr = "list = Cons 3 (Cons 2 (Cons 1 Nil)); main = length list";
            // var progStr = "list = Cons 3 (Cons 2 (Cons 1 Nil)); main = head list";
            // var progStr = "list = Nil; main = head list";
            // var progStr = "list = Cons 3 (Cons 2 (Cons 1 Nil)); main = tail list";
            // var progStr = "main = print 10 stop";
            // var progStr = "list = Cons 3 (Cons 2 (Cons 1 Nil)); main = printList list";

            var tokens = Lexer.lex(progStr);
            var prog = Parser.parse(tokens);

            var timer = new Stopwatch();

            // template instantiation
            {
                timer.Restart();
                var compiler = new TiCompiler();
                var initialState = compiler.Compile(prog);
                timer.Stop();
                Console.WriteLine($"Ti: Compilation took {timer.Elapsed.TotalMilliseconds:0.00}ms");

                timer.Restart();
                var evaluator = new TiEvaluator();
                var states = evaluator.Evaluate(initialState).ToList();
                timer.Stop();
                Console.WriteLine($"Ti: Evaluation took {timer.Elapsed.TotalMilliseconds:0.00}ms");

                Console.WriteLine($"Ti: Took {states.Count} states and {states.Last().Heap.Count} heap entries");
                var finalState = states.Last();
                Console.WriteLine(TiStatePrinter.Print(finalState));

                // DrawAllStates(states);
            }

            //  g-machine
            {
                timer.Restart();
                var compiler = new GmCompiler();
                var initialState = compiler.Compile(prog);
                timer.Stop();
                Console.WriteLine($"Gm: Compilation took {timer.Elapsed.TotalMilliseconds:0.00}ms");

                timer.Restart();
                var evaluator = new GmEvaluator();
                var states = evaluator.Evaluate(initialState).ToList();
                timer.Stop();
                Console.WriteLine($"Gm: Evaluation took {timer.Elapsed.TotalMilliseconds:0.00}ms");

                Console.WriteLine($"Gm: Took {states.Count} states and {states.Last().Heap.Count} heap entries");
                var finalState = states.Last();
                Console.WriteLine(GmStatePrinter.Print(finalState));
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
