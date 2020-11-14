using FuncComp.Language;
using static FuncComp.Helpers.LanguageFactory;
using static FuncComp.Helpers.LanguageFactory<FuncComp.Language.Name>;

namespace FuncComp.Helpers
{
    public static class Prelude
    {
        public static Program<Name> Program = new Program<Name>(new []
        {
            // I x = x
            ScDef(N("I"), new []{ N("x") }, Var(N("x"))),
            // K x y = x
            ScDef(N("K"), new []{ N("x"), N("y") }, Var(N("x"))),
            // K1 x y = y
            ScDef(N("K1"), new []{ N("x"), N("y") }, Var(N("y"))),
            // S f g x = f x (g x)
            ScDef(N("S"), new [] { N("f"), N("g"), N("x") }, Ap(Ap(Var(N("f")), Var(N("x"))), Ap(Var(N("g")), Var(N("x"))))),
            // compose f g x = f (g x)
            ScDef(N("compose"), new [] { N("f"), N("g"), N("x") }, Ap(Var(N("f")), Ap(Var(N("g")), Var(N("x"))))),
            // twice f = compose f f
            ScDef(N("twice"), new []{ N("f") }, Ap(Ap(Var(N("compose")), Var(N("f"))), Var(N("f")))),
        });
    }
}