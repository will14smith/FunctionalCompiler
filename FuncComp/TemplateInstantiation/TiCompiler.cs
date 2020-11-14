using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FuncComp.Helpers;
using FuncComp.Language;

namespace FuncComp.TemplateInstantiation
{
    public class TiCompiler
    {
        public IEnumerable<SupercombinatorDefinition<Name>> ExtraPrelude { get; } = Enumerable.Empty<SupercombinatorDefinition<Name>>();

        public TiState Compile(Program<Name> program)
        {
            var supercombinatorDefs = program.Supercombinators.Concat(Prelude.Program.Supercombinators).Concat(ExtraPrelude);

            var (initialHeap, globals) = BuildInitialHeap(supercombinatorDefs);

            var initialStack = ImmutableStack<int>.Empty.Push(globals[new Name("main")]);

            return new TiState(initialStack, initialHeap.ToImmutableDictionary(), globals.ToImmutableDictionary());
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

            return (heap, globals);
        }
    }
}