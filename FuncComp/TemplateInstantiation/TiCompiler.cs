using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FuncComp.Helpers;
using FuncComp.Language;
using static FuncComp.Helpers.LanguageFactory;
using static FuncComp.Helpers.LanguageFactory<FuncComp.Language.Name>;

namespace FuncComp.TemplateInstantiation
{
    public class TiCompiler
    {
        private static readonly Expression<Name> False = Pack(1, 0);
        private static readonly Expression<Name> True = Pack(2, 0);

        private readonly IEnumerable<SupercombinatorDefinition<Name>> _extraPrelude = new []
        {
            ScDef(N("not"), new [] { N("x") }, ApM(Var("if"), Var("x"), False, True)),
            ScDef(N("and"), new [] { N("x"), N("y") }, ApM(Var("if"), Var("x"), Var("y"), False)),
            ScDef(N("or"), new [] { N("x"), N("y") }, ApM(Var("if"), Var("x"), True, Var("y"))),

            ScDef(N("MkPair"), new Name[0], Pack(1, 2)),
            ScDef(N("fst"), new [] { N("p") }, ApM(Var("casePair"), Var("p"), Var("K"))),
            ScDef(N("snd"), new [] { N("p") }, ApM(Var("casePair"), Var("p"), Var("K1"))),
        };
        private readonly IReadOnlyDictionary<Name, PrimitiveType> _primitives = new Dictionary<Name, PrimitiveType>
        {
            { new Name("negate"), new PrimitiveType.Neg() },
            { new Name("+"), new PrimitiveType.Add() },
            { new Name("-"), new PrimitiveType.Sub() },
            { new Name("*"), new PrimitiveType.Mul() },
            { new Name("/"), new PrimitiveType.Div() },

            { new Name(">"), new PrimitiveType.Greater() },
            { new Name(">="), new PrimitiveType.GreaterEqual() },
            { new Name("<"), new PrimitiveType.Less() },
            { new Name("<="), new PrimitiveType.LessEqual() },
            { new Name("=="), new PrimitiveType.Equal() },
            { new Name("~="), new PrimitiveType.NotEqual() },

            { new Name("if"), new PrimitiveType.If() },
            { new Name("casePair"), new PrimitiveType.CasePair() },
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