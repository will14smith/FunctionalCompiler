using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace FuncComp.Helpers
{
    public static class ImmutableExtensions
    {
        public static (ImmutableStack<T> Stack, IReadOnlyList<T> Items) PopMultiple<T>(this ImmutableStack<T> stack, int count)
        {
            var items = new T[count];

            for (var i = 0; i < count; i++)
            {
                if (stack.IsEmpty)
                {
                    throw new Exception("Stack has too few items");
                }

                stack = stack.Pop(out var value);

                items[i] = value;
            }

            return (stack, items);
        }

        public static ImmutableStack<T> Replace<T>(this ImmutableStack<T> stack, T newValue) => stack.Pop().Push(newValue);
    }
}