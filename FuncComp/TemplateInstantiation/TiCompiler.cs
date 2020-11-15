using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FuncComp.Helpers;
using FuncComp.Language;

namespace FuncComp.TemplateInstantiation
{
    public class TiCompiler
    {
        private readonly IEnumerable<SupercombinatorDefinition<Name>> _extraPrelude = Enumerable.Empty<SupercombinatorDefinition<Name>>();
        private readonly IReadOnlyDictionary<Name, PrimitiveType> _primitives = new Dictionary<Name, PrimitiveType>
        {
            { new Name("negate"), PrimitiveType.Neg },
            { new Name("+"), PrimitiveType.Add },
            { new Name("-"), PrimitiveType.Sub },
            { new Name("*"), PrimitiveType.Mul },
            { new Name("/"), PrimitiveType.Div },
        };

        public TiState Compile(Program<Name> program)
        {
            var supercombinatorDefs = program.Supercombinators.Concat(Prelude.Program.Supercombinators).Concat(_extraPrelude);

            var (initialHeap, globals) = BuildInitialHeap(supercombinatorDefs);

            var initialStack = ImmutableStack<int>.Empty.Push(globals[new Name("main")]);
            var initialDump = ImmutableStack<ImmutableStack<int>>.Empty;

            return new TiState(initialStack, initialDump, initialHeap.ToImmutableDictionary(), globals.ToImmutableDictionary());
        }

        private (IReadOnlyDictionary<int, TiNode> Heap, IReadOnlyDictionary<Name, int> Globals) BuildInitialHeap(IEnumerable<SupercombinatorDefinition<Name>> supercombinatorDefs)
        {
            var heap = new Dictionary<int, TiNode>();
            var globals = new Dictionary<Name, int>();

            foreach (var def in supercombinatorDefs)
            {
                var addr = heap.Count;

                heap[addr] = new TiNode.Supercombinator(def.Name, def.Parameters, def.Body);
                globals[def.Name] = addr;
            }

            foreach (var (name, type) in _primitives)
            {
                var addr = heap.Count;

                heap[addr] = new TiNode.Primitive(name, type);
                globals[name] = addr;
            }

            return (heap, globals);
        }
    }
}