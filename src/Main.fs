module App

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Browser.Dom

type Transaction = {
    Description: string
    Amount: float
    IsExpense: bool
}

type Model = {
    Transactions: Transaction list
    Description: string
    Amount: string
    IsExpense: bool
}

type Msg =
    | SetDescription of string
    | SetAmount of string
    | ToggleIsExpense
    | AddTransaction

let init() = 
    { 
        Transactions = []
        Description = ""
        Amount = ""
        IsExpense = true 
    }, Cmd.none

let update msg model =
    match msg with
    | SetDescription desc -> { model with Description = desc }, Cmd.none
    | SetAmount amt -> { model with Amount = amt }, Cmd.none
    | ToggleIsExpense -> { model with IsExpense = not model.IsExpense }, Cmd.none
    | AddTransaction ->
        match System.Double.TryParse(model.Amount) with
        | true, amount when model.Description <> "" ->
            let newTx = {
                Description = model.Description
                Amount = amount
                IsExpense = model.IsExpense
            }
            { model with
                Transactions = newTx :: model.Transactions
                Description = ""
                Amount = ""
            }, Cmd.none
        | _ -> model, Cmd.none

let view model dispatch =
    div [] [
        h1 [] [ str "BudgetBuddy" ]
        div [] [
            input [
                Placeholder "Description"
                Value model.Description
                OnChange (fun ev -> dispatch (SetDescription ev.Value))
            ]
            br []
            input [
                Placeholder "Amount"
                Value model.Amount
                OnChange (fun ev -> dispatch (SetAmount ev.Value))
            ]
            br []
            label [] [
                input [
                    Type "checkbox"
                    Checked (not model.IsExpense)
                    OnChange (fun _ -> dispatch ToggleIsExpense)
                ]
                str " Income"
            ]
            br []
            button [OnClick (fun _ -> dispatch AddTransaction)] [str "Add Transaction"]
        ]
        hr []
        h2 [] [str "Transactions"]
        ul [] [
            yield! model.Transactions |> List.map (fun tx ->
        str (sprintf "%s%.2f %s" (if tx.IsExpense then "-" else "+") tx.Amount tx.Description)        ]
    ]

Program.mkProgram init update view
|> Program.withReact "elmish-app"
|> Program.run