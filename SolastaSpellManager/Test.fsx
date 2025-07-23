open Microsoft.FSharp.Quotations

module M =
    let Func (x: float) = x + x

let nameOf (ex: Expr<_>) =
    match ex with
    | Patterns.Let(_, _, DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _))) -> mi.Name
    | Patterns.Let(a, _, _) -> a.Name
    | Patterns.PropertyGet(_, mi, _) -> mi.Name
    | DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _)) -> mi.Name
    | _ -> failwithf "unexpected expr for nameOf %A" ex

let methodInfo (ex: Expr<_>) =
    match ex with
    | Patterns.Let(_, _, DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _))) -> mi
    | DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _)) -> mi
    | _ -> failwithf "unexpected expr for methodInfo %A" ex

// let f1 = 2
let f2 (i: int) = i * 2

// printfn "name = %A" (methodInfo <@ f1 @>)
printfn "name = %A" (methodInfo <@ f2 @>)
printfn "name = %A" (methodInfo <@ M.Func @>)
