module FuncComp.Parsing.Parser

open FuncComp.Language
open FuncComp.Parsing.Combinators

let private keywords = Set.ofList [ "let"; "letrec"; "case"; "in"; "of"; "Pack" ]

let private tokenValue token = match token with
                               | Token.Identifier s -> s
                               | Token.Number n -> n |> string
                               | Token.Other s -> s
let private lit value = sat (fun t -> value = (tokenValue t))
let private var = sat (fun t -> match t with | Identifier ident -> keywords.Contains ident |> not | _ -> false) |> apply <| (tokenValue >> Name)
let private num = sat (fun t -> match t with | Number _ -> true | _ -> false) |> apply <| fun t -> match t with | Number value -> value | _ -> failwith "invalid"

let private tokenToVar token = tokenValue token |> Name |> Expression<Name>.Variable

let private  relop = altM [lit "<"; lit "<="; lit "=="; lit "~="; lit ">="; lit ">" ]
let private apReduce exprs : Expression<Name> = Seq.reduce (fun a b -> upcast Expression<Name>.Application (a, b)) exprs

let private alternativeTag = bind3 (fun _ tag _ -> tag) (lit "<") num (lit ">")

let rec private expression tokens : (Expression<Name> * Token list) seq = tokens |> altM [
        bind4 (fun _ defs _ expr -> upcast Expression<Name>.Let (false, defs |> Seq.toList, expr)) (lit "let") definitions (lit "in") expression
        bind4 (fun _ defs _ expr -> upcast Expression<Name>.Let (true, defs |> Seq.toList, expr)) (lit "letrec") definitions (lit "in") expression
        bind4 (fun _ expr _ alts -> upcast Expression<Name>.Case (expr, alts |> Seq.toList)) (lit "case") expression (lit "of") alternatives
        expression1
    ]
    and private expression1 tokens : (Expression<Name> * Token list) seq = tokens |> altM [
        bind3 (fun l op r -> upcast Expression<Name>.Application (Expression<Name>.Application (op |> tokenToVar, l), r)) expression2 (lit "|") expression1
        expression2
    ]
    and private expression2 tokens : (Expression<Name> * Token list) seq = tokens |> altM [
        bind3 (fun l op r -> upcast Expression<Name>.Application (Expression<Name>.Application (op |> tokenToVar, l), r)) expression3 (lit "&") expression2
        expression3
    ]
    and private expression3 tokens : (Expression<Name> * Token list) seq = tokens |> altM [
        bind3 (fun l op r -> upcast Expression<Name>.Application (Expression<Name>.Application (op |> tokenToVar, l), r)) expression4 (relop) expression4
        expression4
    ]
    and private expression4 tokens : (Expression<Name> * Token list) seq = tokens |> altM [
        bind3 (fun l op r -> upcast Expression<Name>.Application (Expression<Name>.Application (op |> tokenToVar, l), r)) expression5 (lit "+") expression4
        bind3 (fun l op r -> upcast Expression<Name>.Application (Expression<Name>.Application (op |> tokenToVar, l), r)) expression5 (lit "-") expression5
        expression5
    ]
    and private expression5 tokens : (Expression<Name> * Token list) seq = tokens |> altM [
        bind3 (fun l op r -> upcast Expression<Name>.Application (Expression<Name>.Application (op |> tokenToVar, l), r)) expression6 (lit "*") expression5
        bind3 (fun l op r -> upcast Expression<Name>.Application (Expression<Name>.Application (op |> tokenToVar, l), r)) expression6 (lit "/") expression6
        expression6
    ]
    and private expression6 tokens : (Expression<Name> * Token list) seq = tokens |> apply (oneOrMore atomicExpression) apReduce
    and private atomicExpression tokens : (Expression<Name> * Token list) seq = tokens |> altM [
        apply var (fun n -> upcast Expression<Name>.Variable n)
        apply num (fun n -> upcast Expression<Name>.Number n)
        // TODO Pack { num , num }
        bind3 (fun _ expr _ -> expr) (lit "(") expression (lit ")")
    ]
    and private alternative = bind4 (fun tag parameters _ body -> Alternative<Name> (tag, parameters |> Seq.toList, body)) alternativeTag (zeroOrMore var) (lit "->") expression
    and private alternatives = oneOrMoreWithSep alternative (lit ";")
    and private definition = bind3 (fun name _ expr -> struct (name, expr)) var (lit "=") expression
    and private definitions = oneOrMoreWithSep definition (lit ";")


let private supercombinatorDefinition : Parser<SupercombinatorDefinition<Name>> =
    bind4 (fun a b _ c -> SupercombinatorDefinition<Name> (a, b |> Seq.toList, c)) var (zeroOrMore var) (lit "=") expression

let private program = apply (oneOrMoreWithSep supercombinatorDefinition (lit ";")) (fun scDefs -> Program<Name> (scDefs |> Seq.toList))

let parse (tokens : Token seq) : Program<Name> =
    program (tokens |> Seq.toList) |> Seq.filter (fun (_, remaining) -> List.isEmpty remaining) |> Seq.head |> fst