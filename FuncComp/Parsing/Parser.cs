using System;
using System.Collections.Generic;
using System.Linq;
using FuncComp.Language;

namespace FuncComp.Parsing
{
    public class Parser
    {
        private delegate IEnumerable<(TResult Result, State State)> ParserFn<TResult>(in State state);

        private static readonly IReadOnlyCollection<string> Keywords = new HashSet<string> { "let", "letrec", "case", "in", "of", "Pack" };

        public Program<Name> Parse(IEnumerable<Token> tokens)
        {
            var state = new State(tokens.ToList());

            var valueTuples = Program(state).ToList();
            return valueTuples.First(x => x.State.IsEof).Result;
        }

        // pretty specific
        private static readonly ParserFn<Name> Variable = Apply(Sat(x => x.Type == TokenType.Identifier && !Keywords.Contains(x.Value)), x => new Name(x.Value));
        private static readonly ParserFn<int> Number = Apply(Type(TokenType.Number), x => int.Parse(x.Value));

        private static ParserFn<Expression<Name>> LazyExpression() => (in State state) => Expression(state);

        private static readonly ParserFn<Token> RelationalOp = Alt(Literal("<"), Literal("<="), Literal("=="), Literal("~="), Literal(">="), Literal(">"));

        private static readonly ParserFn<int> AlternativeTag = Then((_, tag, __) => tag, Literal("<"),Number, Literal(">"));
        private static readonly ParserFn<Alternative<Name>> Alternative = Then((tag, parameters, _, body) => new Alternative<Name>(tag, parameters.ToList(), body), AlternativeTag, ZeroOrMore(Variable), Literal("->"), LazyExpression());
        private static readonly ParserFn<IEnumerable<Alternative<Name>>> Alternatives = OneOrMoreWithSep(Alternative, Literal(";"));
        private static readonly ParserFn<(Name, Expression<Name>)> Definition = Then((name, _, expr) => (name, expr), Variable, Literal("="), LazyExpression());
        private static readonly ParserFn<IEnumerable<(Name, Expression<Name>)>> Definitions = OneOrMoreWithSep(Definition, Literal(";"));
        private static readonly ParserFn<Expression<Name>> AtomicExpression = Alt(
            Apply(Variable, x => (Expression<Name>) new Expression<Name>.Variable(x)),
            Apply(Number, x => (Expression<Name>) new Expression<Name>.Number(x)),
            // TODO Pack { num , num }
            Then((_, expr, __) => expr, Literal("("), LazyExpression(), Literal(")"))
        );

        private static readonly ParserFn<Expression<Name>> Expression6 = Apply(OneOrMore(AtomicExpression), Application);
        private static readonly ParserFn<Expression<Name>> Expression5 = Alt(
            Then((l, op, r) => BinaryOp(op, l, r), Expression6, Literal("*"), (in State state) => Expression5(state)),
            Then((l, op, r) => BinaryOp(op, l, r), Expression6, Literal("/"), Expression6),
            Expression6
        );
        private static readonly ParserFn<Expression<Name>> Expression4 = Alt(
            Then((l, op, r) => BinaryOp(op, l, r), Expression5, Literal("+"), (in State state) => Expression4(state)),
            Then((l, op, r) => BinaryOp(op, l, r), Expression5, Literal("-"), Expression5),
            Expression5
        );
        private static readonly ParserFn<Expression<Name>> Expression3 = Alt(
            Then((l, op, r) => BinaryOp(op, l, r), Expression4, RelationalOp, Expression4),
            Expression4
        );
        private static readonly ParserFn<Expression<Name>> Expression2 = Alt(
            Then((l, op, r) => BinaryOp(op, l, r), Expression3, Literal("&"), (in State state) => Expression2(state)),
            Expression3
        );
        private static readonly ParserFn<Expression<Name>> Expression1 = Alt(
            Then((l, op, r) => BinaryOp(op, l, r), Expression2, Literal("|"), (in State state) => Expression1(state)),
            Expression2
        );

        private static Expression<Name> Application(IEnumerable<Expression<Name>> expressions) => expressions.Aggregate((a, b) => new Expression<Name>.Application(a, b));
        private static Expression<Name> BinaryOp(Token op, Expression<Name> left, Expression<Name> right) => new Expression<Name>.Application(new Expression<Name>.Application(new Expression<Name>.Variable(new Name(op.Value)), left),right);

        private static readonly ParserFn<Expression<Name>> Expression = Alt(
            Then((_, definitions, __, body) => (Expression<Name>) new Expression<Name>.Let(false, definitions.ToList(), body), Literal("let"), Definitions, Literal("in"), LazyExpression()),
            Then((_, definitions, __, body) => (Expression<Name>) new Expression<Name>.Let(true, definitions.ToList(), body), Literal("letrec"), Definitions, Literal("in"), LazyExpression()),
            Then((_, expr, __, alternatives) => (Expression<Name>) new Expression<Name>.Case(expr, alternatives.ToList()), Literal("case"), LazyExpression(), Literal("of"), Alternatives),
            Then((_, parameters, __, body) => (Expression<Name>) new Expression<Name>.Lambda(parameters.ToList(), body), Literal("\\"), OneOrMore(Variable), Literal("of"), LazyExpression()),
            Expression1
        );

        private static readonly ParserFn<SupercombinatorDefinition<Name>> SupercombinatorDefinition = Then((name, parameters, _, body) => new SupercombinatorDefinition<Name>(name, parameters.ToList(), body), Variable, ZeroOrMore(Variable), Literal("="), Expression);
        private static readonly ParserFn<Program<Name>> Program = Apply(OneOrMoreWithSep(SupercombinatorDefinition, Literal(";")), scDefs => new Program<Name>(scDefs.ToList()));

        // mostly generic
        private static ParserFn<Token> Literal(string literal) => Sat(x => x.Value == literal);
        private static ParserFn<Token> Type(TokenType type) => Sat(x => x.Type == type);

        // total generic
        private static ParserFn<Token> Sat(Func<Token, bool> pred) => (in State state) => !state.IsEof && pred(state.Current) ? new[] {(state.Current, state.Consume())} : new (Token, State)[] { };
        private static ParserFn<TResult> Alt<TResult>(params ParserFn<TResult>[] ps) => (in State state) => { var innerState = state; return ps.SelectMany(p => p(innerState)); };

        private static ParserFn<TResult> Then<TP1, TP2, TResult>(Func<TP1, TP2, TResult> combine, ParserFn<TP1> p1, ParserFn<TP2> p2) => (in State state) =>
            p1(state).SelectMany(x => p2(x.State).Select(y => (combine(x.Result, y.Result), y.State)));
        private static ParserFn<TResult> Then<TP1, TP2, TP3, TResult>(Func<TP1, TP2, TP3, TResult> combine, ParserFn<TP1> p1, ParserFn<TP2> p2, ParserFn<TP3> p3) =>
            (in State state) => p1(state).SelectMany(x => p2(x.State).SelectMany(y => p3(y.State).Select(z => (combine(x.Result, y.Result, z.Result), z.State))));
        private static ParserFn<TResult> Then<TP1, TP2, TP3, TP4, TResult>(Func<TP1, TP2, TP3, TP4, TResult> combine, ParserFn<TP1> p1, ParserFn<TP2> p2, ParserFn<TP3> p3, ParserFn<TP4> p4) =>
            (in State state) => p1(state).SelectMany(r1 => p2(r1.State).SelectMany(r2 => p3(r2.State).SelectMany(r3 => p4(r3.State).Select(r4 => (combine(r1.Result, r2.Result, r3.Result, r4.Result), r4.State)))));

        private static ParserFn<TP2> Apply<TP1, TP2>(ParserFn<TP1> p, Func<TP1, TP2> func) =>
            (in State state) => p(state).Select(r => (func(r.Result), r.State));

        private static ParserFn<TResult> Empty<TResult>(TResult result) => (in State state) => new[] { (result, state) };
        private static ParserFn<IEnumerable<TResult>> ZeroOrMore<TResult>(ParserFn<TResult> p) => Alt(OneOrMore(p), Empty(Enumerable.Empty<TResult>()));
        private static ParserFn<IEnumerable<TResult>> OneOrMore<TResult>(ParserFn<TResult> p) => Then((a, b) => b.Prepend(a), p, (in State state) => ZeroOrMore(p)(state));

        private static ParserFn<IEnumerable<TResult>> OneOrMoreWithSep<TResult, TSep>(ParserFn<TResult> p, ParserFn<TSep> sep) => Then((a, b) => b.Prepend(a), p, ZeroOrMore(Then((_, r) => r, sep, p)));

        private struct State
        {
            public IReadOnlyList<Token> Tokens { get; }
            public int Offset { get; }

            public Token Current => Tokens[Offset];
            public bool IsEof => Offset >= Tokens.Count;

            public State(IReadOnlyList<Token> tokens, int offset = 0)
            {
                Tokens = tokens;
                Offset = offset;
            }

            public readonly State Consume()
            {
                return new State(Tokens, Offset + 1);
            }
        }
    }
}