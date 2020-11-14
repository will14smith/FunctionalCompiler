namespace FuncComp.Language
{
    public abstract partial class Expression<TId>
    {
        public abstract override bool Equals(object? obj);
        public abstract override int GetHashCode();
        public abstract override string ToString();

        public abstract TResult Accept<TResult, TState>(IExpressionVisitor<TId, TState, TResult> visitor, TState state);
    }
}