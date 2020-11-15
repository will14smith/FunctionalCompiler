module FuncComp.Parsing.Combinators

type Parser<'a> = Token list -> ('a * Token list) seq

let sat pred tokens =
    match tokens with
    | token :: rest -> if pred token then Seq.singleton (token, rest) else Seq.empty
    | _ -> Seq.empty

let alt (p1 : Parser<'a>) (p2 : Parser<'a>) tokens = Seq.append (p1 tokens) (p2 tokens)

let altM ( ps : Parser<'a> list) (tokens : Token List) = ps |> Seq.map (fun a -> a tokens) |> Seq.concat

let bind (f : 'a -> 'b -> 'c)  (p1 : Parser<'a>) (p2 : Parser<'b>) tokens = seq {
    for (v1, tokens) in p1 tokens do
    for (v2, tokens) in p2 tokens do
        (f v1 v2, tokens)
}
let bind3 (f : 'a -> 'b -> 'c -> 'd)  (p1 : Parser<'a>) (p2 : Parser<'b>) (p3 : Parser<'c>) tokens = seq {
    for (v1, tokens) in p1 tokens do
    for (v2, tokens) in p2 tokens do
    for (v3, tokens) in p3 tokens do
        (f v1 v2 v3, tokens)
}
let bind4 (f : 'a -> 'b -> 'c -> 'd -> 'e)  (p1 : Parser<'a>) (p2 : Parser<'b>) (p3 : Parser<'c>) (p4 : Parser<'d>) tokens = seq {
    for (v1, tokens) in p1 tokens do
    for (v2, tokens) in p2 tokens do
    for (v3, tokens) in p3 tokens do
    for (v4, tokens) in p4 tokens do
        (f v1 v2 v3 v4, tokens)
}

let apply (p : Parser<'a>) (f : 'a -> 'b) tokens = p tokens |> Seq.map (fun (a, ts) -> (f a, ts))

let empty value tokens = Seq.singleton (value, tokens)
let rec zeroOrMore p = alt (oneOrMore p) (empty Seq.empty)
    and oneOrMore p = bind (fun a b -> Seq.append [a] b) p (zeroOrMore p)

let oneOrMoreWithSep (p : Parser<'a>) (s : Parser<'b>) tokens = bind (fun a b -> Seq.append [a] b) p (zeroOrMore <| bind (fun _ b -> b) s p) tokens
