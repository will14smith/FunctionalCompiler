using System.Collections.Generic;
using System.Linq;
using FuncComp.Language;

namespace FuncComp.Helpers
{
    public static class LanguageFactory
    {
        public static Name N(string s) => new Name(s);
    }

    public static class LanguageFactory<T>
    {
        public static SupercombinatorDefinition<T> ScDef(Name name, IReadOnlyCollection<T> parameters, Expression<T> body) => new SupercombinatorDefinition<T>(name, parameters, body);

        public static Expression<T> Var(Name name) => new Expression<T>.Variable(name);
        public static Expression<T> Var(string name) => Var(new Name(name));
        public static Expression<T> Num(int value) => new Expression<T>.Number(value);
        public static Expression<T> Ap(Expression<T> function, Expression<T> parameter) => new Expression<T>.Application(function, parameter);
        public static Expression<T> ApM(Expression<T> function, params Expression<T>[] parameters) => parameters.Aggregate(function, (agg, node) => new Expression<T>.Application(agg, node));
        public static Expression<T> Let(Expression<T> body, params (T, Expression<T>)[] definitions) => new Expression<T>.Let(false, definitions, body);
        public static Expression<T> LetRec(Expression<T> body, params (T, Expression<T>)[] definitions) => new Expression<T>.Let(true, definitions, body);
    }
}