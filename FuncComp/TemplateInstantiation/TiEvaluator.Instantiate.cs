using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FuncComp.Language;

namespace FuncComp.TemplateInstantiation
{
    public partial class TiEvaluator
    {
        private static (ImmutableDictionary<int, TiNode>, int) Instantiate(Expression<Name> expr, ImmutableDictionary<int, TiNode> heap, ImmutableDictionary<Name, int> env, int? target)
        {
            switch (expr)
            {
                case Expression<Name>.Number num:
                    return AssignOrAllocate(target, heap, new TiNode.Number(num.Value));

                case Expression<Name>.Application ap:
                    var (heap1, a1) = Instantiate(ap.Function, heap, env, null);
                    var (heap2, a2) = Instantiate(ap.Parameter, heap1, env, null);

                    return AssignOrAllocate(target, heap2, new TiNode.Application(a1, a2));

                case Expression<Name>.Variable variable:
                    if (target.HasValue)
                    {
                        return (Assign(target.Value, heap, new TiNode.Indirection(env[variable.Name])), target.Value);
                    }

                    return (heap, env[variable.Name]);

                case Expression<Name>.Let let when !let.IsRecursive:
                {
                    var defns = new Dictionary<Name, int>();
                    foreach (var definition in let.Definitions)
                    {
                        var (newHeap, newAddr) = Instantiate(definition.Item2, heap, env, null);
                        defns.Add(definition.Item1, newAddr);
                        heap = newHeap;
                    }

                    var newEnv = env.SetItems(defns);

                    return Instantiate(let.Body, heap, newEnv, null);
                }

                case Expression<Name>.Let let when let.IsRecursive:
                {
                    foreach (var (name, _) in let.Definitions)
                    {
                        var (newHeap, placeholder) = Allocate(heap, new TiNode.Indirection(-1));
                        heap = newHeap;
                        env = env.SetItem(name, placeholder);
                    }

                    foreach (var (name, defExpr) in let.Definitions)
                    {
                        var targetAddr = env[name];
                        (heap, _) = Instantiate(defExpr, heap, env, targetAddr);
                    }

                    return Instantiate(let.Body, heap, env, null);
                }

                case Expression<Name>.Constructor constr:
                    var node = new TiNode.Primitive(new Name("Pack"), new PrimitiveType.Constructor(constr.Tag, constr.Arity));

                    return AssignOrAllocate(target, heap, node);

                default: throw new ArgumentOutOfRangeException(nameof(expr));
            }
        }

        private static (ImmutableDictionary<int, TiNode>, int) AssignOrAllocate(int? addr, ImmutableDictionary<int, TiNode> heap, TiNode node)
        {
            return addr.HasValue ? (Assign(addr.Value, heap, node), addr.Value) : Allocate(heap, node);
        }

        private static ImmutableDictionary<int, TiNode> Assign(int addr, ImmutableDictionary<int, TiNode> heap, TiNode node)
        {
            return heap.SetItem(addr, node);
        }

        private static (ImmutableDictionary<int, TiNode>, int) Allocate(ImmutableDictionary<int, TiNode> heap, TiNode node)
        {
            var addr = heap.Keys.Max() + 1;
            var newHeap = heap.Add(addr, node);

            return (newHeap, addr);
        }
    }
}