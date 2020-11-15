module FuncComp.Parsing.Lexer

open System;

type State = string * int

let private current ((input, offset) : State) = input.[offset]
let private next ((input, offset) : State) = (input, offset + 1)
let private isEof ((input, offset) : State) = offset >= input.Length

let private takeWhile (func : char -> bool) (state : State) =
    let rec nextUntil (state : State) =
        if isEof state then state
        else if current state |> func then nextUntil <| next state
        else state

    let (input, startOffset) = state
    let (_, endOffset) = nextUntil state

    (input.Substring(startOffset, endOffset - startOffset), (input, endOffset))

let private (|WhiteSpace|_|) (state : State) = if current state |> Char.IsWhiteSpace then next state |> Some else None
let private (|NumberStart|_|) (state : State) = if current state |> Char.IsDigit then state |> Some else None
let private (|IdentifierStart|_|) (state : State) = if current state |> Char.IsLetter then state |> Some else None
let private (|Literal|_|) (lit : string) ((input, offset) : State) =
    if offset + lit.Length >= input.Length then None
    else if lit = input.Substring(offset, lit.Length) then Some (lit, (input, offset + lit.Length))
    else None

let private isIdent c = Char.IsLetter(c) || Char.IsDigit(c) || c = '_'

let lex (input : string) : Token seq =
    let lexNumber (state : State) =
        let (str, state) = takeWhile Char.IsDigit state
        ([str |> int |> Token.Number], state)
    let lexIdent (state : State) =
        let (str, state) = takeWhile isIdent state
        ([str |> Token.Identifier], state)

    let rec lex (state : State) : Token seq =
        if isEof state then Seq.empty
        else match state with
             | WhiteSpace s -> lex s
             | NumberStart s ->
                    let (tokens, s) = lexNumber s
                    Seq.append tokens <| lex s
             | IdentifierStart s ->
                    let (tokens, state) = lexIdent state
                    Seq.append tokens <| lex state
             | Literal "||" (_, s) -> takeWhile (fun c -> c <> '\n') s |> snd |> lex
             | Literal "==" (v, s) -> Seq.append [Token.Other v] (lex s)
             | Literal "~=" (v, s) -> Seq.append [Token.Other v] (lex s)
             | Literal ">=" (v, s) -> Seq.append [Token.Other v] (lex s)
             | Literal "<=" (v, s) -> Seq.append [Token.Other v] (lex s)
             | Literal "->" (v, s) -> Seq.append [Token.Other v] (lex s)
             | s -> Seq.append [current s |> string |> Token.Other] (lex <| next state)

    lex (input, 0)
