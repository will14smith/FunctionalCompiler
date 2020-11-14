using FuncComp.Language;

namespace FuncComp.Helpers
{
    public static class ExpressionExtensions
    {
        public static bool IsAtomic<TId>(this Expression<TId> expr)
        {
            return expr switch
            {
                Expression<TId>.Number _ => true,
                Expression<TId>.Variable _ => true,
                _ => false
            };
        }
    }
}