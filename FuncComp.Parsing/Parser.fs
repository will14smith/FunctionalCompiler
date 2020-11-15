module FuncComp.Parsing.Parser

open FuncComp.Language
open FuncComp.Parsing.Combinators

let private keywords = Set.ofList [ "let"; "letrec"; "case"; "in"; "of"; "Pack" ]

let private tokenValue token = match token with
                               | Token.Identifier s -> s
                               | Token.Number n -> n |> string
                               | Token.Other s -> s
let private lit value = sat (fun t -> value = (tokenValue t)) ("literal: " + value)
let private var = sat (fun t -> match t with | Identifier ident -> keywords.Contains ident |> not | _ -> false) "variable" |>> (tokenValue >> Name)
let private num = sat (fun t -> match t with | Number _ -> true | _ -> false) "number" |>> fun t -> match t with | Number value -> value | _ -> failwith "invalid"

let private strToVar str = str |> Name |> Expression<Name>.Variable
let private tokenToVar token = tokenValue token |> strToVar

let private expression, expressionRef = createParserForwardedToRef<Expression<Name>>()

let private alternativeTag = bind3 (fun _ tag _ -> tag) (lit "<") num (lit ">")
let private alternative = bind4 (fun tag parameters _ body -> Alternative<Name> (tag, parameters |> Seq.toList, body)) alternativeTag (zeroOrMore var) (lit "->") expression
let private alternatives = oneOrMoreWithSep alternative (lit ";")

let private definition = bind3 (fun name _ expr -> struct (name, expr)) var (lit "=") expression
let private definitions = oneOrMoreWithSep definition (lit ";")

let private atomicExpression : Parser<Expression<Name>> = choice [
        var |>> (fun n -> upcast Expression<Name>.Variable n)
        num |>> (fun n -> upcast Expression<Name>.Number n)
        // TODO Pack { num , num }
        bind3 (fun _ expr _ -> expr) (lit "(") expression (lit ")")
    ]

let private apReduce exprs : Expression<Name> = Seq.reduce (fun a b -> upcast Expression<Name>.Application (a, b)) exprs
let private expression6 : Parser<Expression<Name>> = (oneOrMore atomicExpression) |>> apReduce

let private binOpReduce op exprs : Expression<Name> = Seq.reduce (fun a e -> upcast Expression<Name>.Application (Expression<Name>.Application (op |> strToVar , a), e)) exprs

let private expression5 : Parser<Expression<Name>> = choice [
        bind3 (fun l op r -> binOpReduce (op |> tokenValue) [l; r]) expression6 (lit "/") expression6
        oneOrMoreWithSep expression6 (lit "*") |>> binOpReduce("*")
    ]

let private expression4 : Parser<Expression<Name>> = choice [
        bind3 (fun l op r -> binOpReduce (op |> tokenValue) [l; r]) expression5 (lit "-") expression5
        oneOrMoreWithSep expression5 (lit "+") |>> binOpReduce("+")
    ]

let private  relop = choice [lit "<"; lit "<="; lit "=="; lit "~="; lit ">="; lit ">" ]
let private expression3 : Parser<Expression<Name>> = choice [
        bind3 (fun l op r -> binOpReduce (op |> tokenValue) [l; r]) expression5 (relop) expression5
        expression4
    ]

let private expression2 : Parser<Expression<Name>> = oneOrMoreWithSep expression3 (lit "&") |>> binOpReduce("&")

let private expression1 : Parser<Expression<Name>> = oneOrMoreWithSep expression2 (lit "|") |>> binOpReduce("|")

let private expressionActual : Parser<Expression<Name>> = choice [
        bind4 (fun _ defs _ expr -> upcast Expression<Name>.Let (false, defs |> Seq.toList, expr)) (lit "let") definitions (lit "in") expression
        bind4 (fun _ defs _ expr -> upcast Expression<Name>.Let (true, defs |> Seq.toList, expr)) (lit "letrec") definitions (lit "in") expression
        bind4 (fun _ expr _ alts -> upcast Expression<Name>.Case (expr, alts |> Seq.toList)) (lit "case") expression (lit "of") alternatives
        expression1
    ]
expressionRef := expressionActual

let private supercombinatorDefinition : Parser<SupercombinatorDefinition<Name>> =
    bind4 (fun a b _ c -> SupercombinatorDefinition<Name> (a, b |> Seq.toList, c)) var (zeroOrMore var) (lit "=") expression

let private program = oneOrMoreWithSep supercombinatorDefinition (lit ";") |>> (fun scDefs -> Program<Name> (scDefs |> Seq.toList))

let parse (tokens : Token seq) : Program<Name> =
    match run program (tokens |> Seq.toList) with
    | Ok (result, []) -> result
    | Ok (_, tokens) -> failwithf "didn't consume all the tokens: %O" tokens
    | Error (source, message) -> failwithf "%s: %s" source message