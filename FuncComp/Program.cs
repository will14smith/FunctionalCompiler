using System;
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
            var progStr = "pair x y f = f x y; fst p = p K; snd p = p K1; f x y = letrec a = pair x b; b = pair y a in fst (snd (snd (snd a))); main = f 3 4";

            var tokens = Lexer.lex(progStr);
            var prog = Parser.parse(tokens);

            // template instantiation
            {
                var compiler = new TiCompiler();
                var initialState = compiler.Compile(prog);

                var evaluator = new TiEvaluator();
                var states = evaluator.Evaluate(initialState).ToList();

                var finalState = states.Last();
                Console.WriteLine(TiStatePrinter.Print(finalState));
            }
        }
    }
}
