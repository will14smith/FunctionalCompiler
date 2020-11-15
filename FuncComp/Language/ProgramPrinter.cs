using System.Collections.Generic;
using System.Linq;
using FuncComp.Helpers;
using static FuncComp.Helpers.PrettyPrinter.NodeBuilder;

namespace FuncComp.Language
{
    public class ProgramPrinter
    {
        public static string Print(Program<Name> program)
        {
            var scDefs = program.Supercombinators.Select(Build);

            var sep = Str(";\n\n");
            var node = Interleave(sep, scDefs);

            return PrettyPrinter.Display(node);
        }

        public static string Print(SupercombinatorDefinition<Name> scDef)
        {
            return PrettyPrinter.Display(Build(scDef));
        }

        public static string Print(Expression<Name> expr) => PrettyPrinter.Display(expr.Accept(ExpressionPrinter.Instance, null));

        private static PrettyPrinter.Node Build(SupercombinatorDefinition<Name> scDef)
        {
            var header = new[] {scDef.Name}.Concat(scDef.Parameters).Select(x => Str(x.Value));
            var headerNode = Interleave(Str(" "), header);

            var body = scDef.Body.Accept(ExpressionPrinter.Instance, null);

            return Append(headerNode, Str(" = "), Indent(body));
        }

        private class ExpressionPrinter : IExpressionVisitor<Name, object?, PrettyPrinter.Node>
        {
            public static readonly ExpressionPrinter Instance = new ExpressionPrinter();

            public PrettyPrinter.Node VisitApplication(Expression<Name>.Application expr, object? state)
            {
                if (expr.Function is Expression<Name>.Application ap && ap.Function is Expression<Name>.Variable var && IsBinOp(var.Name))
                {
                    return Append(PrintAtomic(ap.Parameter, state), Str($" {var.Name} "), PrintAtomic(expr.Parameter, state));
                }

                return Append(expr.Function.Accept(this, state), Str(" "), PrintAtomic(expr.Parameter, state));
            }

            private static bool IsBinOp(Name var)
            {
                return var.Value == "+" || var.Value == "-" || var.Value == "*" || var.Value == "/"
                       || var.Value == "<" || var.Value == "<=" || var.Value == "==" || var.Value == "~=" || var.Value == ">=" || var.Value == ">"
                       || var.Value == "&" || var.Value == "|";
            }

            public PrettyPrinter.Node VisitCase(Expression<Name>.Case expr, object? state)
            {
                throw new System.NotImplementedException();
            }

            public PrettyPrinter.Node VisitConstructor(Expression<Name>.Constructor expr, object? state) =>
                Append(Str("Pack{"), Num(expr.Tag), Str(", "), Num(expr.Arity), Str("}"));

            public PrettyPrinter.Node VisitLambda(Expression<Name>.Lambda expr, object? state)
            {
                throw new System.NotImplementedException();
            }

            public PrettyPrinter.Node VisitLet(Expression<Name>.Let expr, object? state)
            {
                var keyword = expr.IsRecursive ? "letrec" : "let";
                var definitions = PrintDefinitions(expr.Definitions, state);
                var body = expr.Body.Accept(this, state);

                return Append(
                    Str(keyword),
                    Newline(),

                    Str("  "),
                    PrettyPrinter.NodeBuilder.Indent(definitions),
                    Newline(),

                    Str("in "),
                    body);
            }

            public PrettyPrinter.Node VisitNumber(Expression<Name>.Number expr, object? state)
            {
                return Str(expr.Value.ToString());
            }

            public PrettyPrinter.Node VisitVariable(Expression<Name>.Variable expr, object? state)
            {
                return Str(expr.Name.Value);
            }

            private PrettyPrinter.Node PrintAtomic(Expression<Name> expr, object? state)
            {
                var exprString = expr.Accept(this, state);

                return expr.IsAtomic() ? exprString : Append(Str("("), exprString, Str(")"));
            }
            private PrettyPrinter.Node PrintDefinitions(IReadOnlyCollection<(Name, Expression<Name>)> definitions, object? state)
            {
                var sep = Append(Str(";"), Newline());

                var definitionNodes = definitions.Select(x => PrintDefinition(x, state));

                return Interleave(sep, definitionNodes);
            }
            private PrettyPrinter.Node PrintDefinition((Name Name, Expression<Name> Value) definition, object? state)
            {
                return Append(Str(definition.Name.Value), Str(" = "), Indent(definition.Value.Accept(this, state)));
            }
        }

    }
}