namespace FuncComp.Parsing

type Token =
    | Identifier of string
    | Number of int
    | Other of string

