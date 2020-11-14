using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FuncComp.Helpers
{
    public static class PrettyPrinter
    {
        public static string Display(Node node)
        {
            var output = new StringBuilder();

            var currentColumn = 0;
            var workList = new Stack<(Node, int)>();
            workList.Push((node, 0));

            while (workList.Count > 0)
            {
                var (currentNode, targetOffset) = workList.Pop();

                switch (currentNode)
                {
                    case Node.Nil _: break;
                    case Node.Str str:
                        output.Append(str.Value);
                        currentColumn += str.Value.Length;
                        break;
                    case Node.Append append:
                        workList.Push((append.Right, targetOffset));
                        workList.Push((append.Left, targetOffset));
                        break;
                    case Node.Indent indent:
                        workList.Push((indent.Inner, currentColumn));
                        break;
                    case Node.Newline _:
                        output.Append("\n");
                        output.Append(new string(' ', targetOffset));
                        currentColumn = targetOffset;
                        break;

                    default: throw new ArgumentOutOfRangeException(nameof(currentNode));
                }
            }

            return output.ToString();
        }

        public class NodeBuilder
        {
            public static Node Nil() => new Node.Nil();
            public static Node Str(string value) => Interleave(new Node.Newline(), value.Split("\n").Select(x => new Node.Str(x)));
            public static Node Append(params Node[] nodes) => nodes.Aggregate((Node)new Node.Nil(), (agg, node) => agg is Node.Nil ? node : node is Node.Nil ? agg : new Node.Append(agg, node));

            public static Node Interleave(Node sep, IEnumerable<Node> nodes)
            {
                return nodes.Aggregate<Node, Node>(new Node.Nil(), (current, node) => current is Node.Nil ? node : Append(current, sep, node));
            }
            public static Node Indent(Node node) => new Node.Indent(node);
            public static Node Newline() => new Node.Newline();
        }

        public abstract class Node
        {
            public class Nil : Node { }
            public class Str : Node
            {
                public Str(string value)
                {
                    Value = value;
                }

                public string Value { get; }
            }

            public class Append : Node
            {
                public Append(Node left, Node right)
                {
                    Left = left;
                    Right = right;
                }

                public Node Left { get; }
                public Node Right { get; }
            }

            public class Indent : Node
            {
                public Indent(Node inner)
                {
                    Inner = inner;
                }

                public Node Inner { get; }
            }

            public class Newline : Node { }
        }
    }
}