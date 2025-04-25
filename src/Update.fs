module BudgetBuddy.Update

open BudgetBuddy.Model

type Msg =
    | AddTransaction of Transaction
    | RemoveTransaction of int
    | LoadFromStorage
    | SaveToStorage

let init () =
    { Transactions = []; NextId = 1 }, Cmd.none

let update msg model =
    match msg with
    | AddTransaction t ->
        let tWithId = { t with Id = model.NextId }
        let updated = tWithId :: model.Transactions
        { model with Transactions = updated; NextId = model.NextId + 1 }, Cmd.none

    | RemoveTransaction id ->
        let updated = model.Transactions |> List.filter (fun t -> t.Id <> id)
        { model with Transactions = updated }, Cmd.none

    | LoadFromStorage ->
        // Placeholder
        model, Cmd.none

    | SaveToStorage ->
        // Placeholder
        model, Cmd.none
