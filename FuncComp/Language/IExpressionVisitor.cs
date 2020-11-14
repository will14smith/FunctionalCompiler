namespace FuncComp.Language
{
    public interface IExpressionVisitor<TId, in TState, out TResult>
    {
         TResult VisitApplication(Expression<TId>.Application expr, TState state);
         TResult VisitCase(Expression<TId>.Case expr, TState state);
         TResult VisitConstructor(Expression<TId>.Constructor expr, TState state);
         TResult VisitLambda(Expression<TId>.Lambda expr, TState state);
         TResult VisitLet(Expression<TId>.Let expr, TState state);
         TResult VisitNumber(Expression<TId>.Number expr, TState state);
         TResult VisitVariable(Expression<TId>.Variable expr, TState state);
    }

    public abstract class StatefulExpressionVisitor<TId, TState> : IExpressionVisitor<TId, TState, object?>
    {
        public abstract void VisitApplication(Expression<TId>.Application expr, TState state);
        public abstract void VisitCase(Expression<TId>.Case expr, TState state);
        public abstract void VisitConstructor(Expression<TId>.Constructor expr, TState state);
        public abstract void VisitLambda(Expression<TId>.Lambda expr, TState state);
        public abstract void VisitLet(Expression<TId>.Let expr, TState state);
        public abstract void VisitNumber(Expression<TId>.Number expr, TState state);
        public abstract void VisitVariable(Expression<TId>.Variable expr, TState state);

        object? IExpressionVisitor<TId, TState, object?>.VisitApplication(Expression<TId>.Application expr, TState state)
        {
            VisitApplication(expr, state);
            return null;
        }

        object? IExpressionVisitor<TId, TState, object?>.VisitCase(Expression<TId>.Case expr, TState state)
        {
            VisitCase(expr, state);
            return null;
        }

        object? IExpressionVisitor<TId, TState, object?>.VisitConstructor(Expression<TId>.Constructor expr, TState state)
        {
            VisitConstructor(expr, state);
            return null;
        }

        object? IExpressionVisitor<TId, TState, object?>.VisitLambda(Expression<TId>.Lambda expr, TState state)
        {
            VisitLambda(expr, state);
            return null;
        }

        object? IExpressionVisitor<TId, TState, object?>.VisitLet(Expression<TId>.Let expr, TState state)
        {
            VisitLet(expr, state);
            return null;
        }

        object? IExpressionVisitor<TId, TState, object?>.VisitNumber(Expression<TId>.Number expr, TState state)
        {
            VisitNumber(expr, state);
            return null;
        }

        object? IExpressionVisitor<TId, TState, object?>.VisitVariable(Expression<TId>.Variable expr, TState state)
        {
            VisitVariable(expr, state);
            return null;
        }
    }
}