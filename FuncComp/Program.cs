using System;
using System.Linq;
using FuncComp.Language;
using FuncComp.Parsing;
using FuncComp.TemplateInstantiation;
using static FuncComp.Helpers.LanguageFactory;
using static FuncComp.Helpers.LanguageFactory<FuncComp.Language.Name>;

namespace FuncComp
{
    class Program
    {
        static void Main(string[] args)
        {
            var let1 = Let(
                ApM(Var("+"), Var("a"), Var("b")),

                (N("a"), Num(7)),
                (N("b"), Num(8)));

            var let2 = Let(
                ApM(Var("+"), Var("a"), ApM(Var("+"), Var("b"), Var("c"))),

                (N("a"), Num(7)),
                (N("b"), Num(8)),
                (N("c"), Num(9)));


            var let3 = Let(
                ApM(Var("+"), Var("a"), let1),
                (N("a"), let1)
            );

            var prog = new Program<Name>(new []
            {
                new SupercombinatorDefinition<Name>(N("f1"), new [] { N("a"), N("b") }, let1),
                new SupercombinatorDefinition<Name>(N("f2"), new [] { N("a"), N("b"), N("c") }, let2),
                new SupercombinatorDefinition<Name>(N("f3"), new [] { N("a"), N("b") }, let3),
            });

            // var progStr = ProgramPrinter.Print(prog);
            // var progStr = "square x = x * x; main = square (square 3)";
            var progStr = "pair x y f = f x y; fst p = p K; snd p = p K1; f x y = letrec a = pair x b; b = pair y a in fst (snd (snd (snd a))); main = f 3 4";

            var lexer = new Lexer();
            var tokens = lexer.Lex(progStr);

            var parser = new Parser();
            var parserProg = parser.Parse(tokens);

            // template instantiation
            {
                var compiler = new TiCompiler();
                var initialState = compiler.Compile(parserProg);

                var evaluator = new TiEvaluator();
                var states = evaluator.Evaluate(initialState).ToList();

                var finalState = states.Last();
                Console.WriteLine(TiStatePrinter.Print(finalState));
            }
        }
    }
}
