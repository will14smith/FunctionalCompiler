module FuncComp.Parsing.Combinators

type ParserLabel = string
type ParserError = string
type Parser<'T> = {
    parse: Token list -> Result<'T * Token list, ParserLabel * ParserError>
    label: ParserLabel
}

let run (parser: Parser<'a>) = parser.parse

let createParserForwardedToRef<'a> () =
    let dummyParser=
        let inner _ : Result<'a * Token list, ParserLabel * ParserError> = failwith "unfixed forwarded parser"
        { parse = inner; label = "unknown" }

    let parserRef = ref dummyParser

    let inner input =
        run !parserRef input
    let wrapperParser = {parse = inner; label = "unknown"}

    wrapperParser, parserRef

let sat (pred : (Token -> bool)) label : Parser<Token> =
    let inner input =
        match input with
        | head::rest when pred head -> Ok (head, rest)
        | _ -> Error (label, "no match")

    { parse = inner; label = label }

let bind (combine : 'a -> 'b -> 'c) (p1 : Parser<'a>) (p2 : Parser<'b>) : Parser<'c> =
    let inner input =
        run p1 input |> Result.bind (fun (v1, input) -> run p2 input |> Result.map (fun (v2, input) -> (combine v1 v2, input)))

    { parse = inner; label = "unknown" }

let bind3 (f : 'a -> 'b -> 'c -> 'd)  (p1 : Parser<'a>) (p2 : Parser<'b>) (p3 : Parser<'c>) : Parser<'d> = bind (fun f c -> f c) (bind f p1 p2) p3
let bind4 (f : 'a -> 'b -> 'c -> 'd -> 'e)  (p1 : Parser<'a>) (p2 : Parser<'b>) (p3 : Parser<'c>) (p4 : Parser<'d>) : Parser<'e> = bind (fun f d -> f d) (bind (fun f c -> f c) (bind f p1 p2) p3) p4

let alt (p1 : Parser<'a>) (p2: Parser<'a>) : Parser<'a> =
    let inner input =
        match run p1 input with
        | Ok result -> Ok result
        | Error _ -> run p2 input

    { parse = inner; label = "unknown" }

let choice ps = List.reduce alt ps

let mapP (f : 'a -> 'b) (p : Parser<'a>) : Parser<'b> =
    let inner input =
        run p input |> Result.map (fun (v, input) -> (f v, input))

    { parse = inner; label = "unknown" }

let returnP (x : 'a) : Parser<'a> =
    let inner input = Ok (x, input)

    { parse = inner; label = "unknown" }

let applyP (fp : Parser<'a -> 'b>) (xp : Parser<'a>) : Parser<'b> = bind (fun f x -> f x) fp xp

let ( .>>. ) p1 p2 = bind (fun a b -> (a,b)) p1 p2
let ( <|> ) p1 p2 = alt p1 p2
let ( <!> ) = mapP
let ( |>> ) x f = mapP f x
let ( <*> ) = applyP

let rec private zeroOrMoreInner (p : Parser<'a>) input =
    match run p input with
    | Ok (value, input) ->
        let (values, input) = zeroOrMoreInner p input
        (value::values, input)
    | Error _ -> ([], input)

let zeroOrMore (p : Parser<'a>) : (Parser<'a list>) =
    let inner = zeroOrMoreInner p >> Result.Ok

    { parse = inner; label = "unknown" }

let oneOrMore (p : Parser<'a>) : (Parser<'a list>) =
    let inner input =
        run p input |> Result.map (fun (value, input) ->
            let (values, input) = zeroOrMoreInner p input
            (value::values, input))

    { parse = inner; label = "unknown" }


let oneOrMoreWithSep (p : Parser<'a>) (s : Parser<'b>) : (Parser<'a list>) =
    let inner input =
        run p input |> Result.map (fun (value, input) ->
            let (values, input) = zeroOrMoreInner (bind (fun _ b -> b) s p) input
            (value::values, input))

    { parse = inner; label = "unknown" }

