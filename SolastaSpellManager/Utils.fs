module Utils

open Microsoft.FSharp.Quotations

let nameOf (ex: Expr<_>) =
    match ex with
    | Patterns.Let(_, _, DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _))) -> mi.Name
    | Patterns.Let(a, _, _) -> a.Name
    | Patterns.PropertyGet(_, mi, _) -> mi.Name
    | DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _)) -> mi.Name
    | _ -> failwithf "unexpected expr %A" ex
