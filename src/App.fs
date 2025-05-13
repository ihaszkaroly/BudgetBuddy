module App

open System
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Browser.Dom

/// Defines whether a transaction is an expense or income
type TransactionType =
    | Expense
    | Income

/// A financial transaction record
type Transaction = {
    Id: Guid
    Date: DateTime
    Description: string
    Amount: float
    Type: TransactionType
}

/// The overall application state
type Model = {
    Transactions: Transaction list
    DescriptionInput: string
    AmountInput: string
    TypeInput: TransactionType
}

/// Messages that update the state
type Msg =
    | UpdateDescription of string
    | UpdateAmount of string
    | UpdateType of TransactionType
    | AddTransaction
    | DeleteTransaction of Guid

/// Initialize with an empty state
let init () : Model * Cmd<Msg> =
    { Transactions = []; DescriptionInput = ""; AmountInput = ""; TypeInput = Expense }, Cmd.none

/// Update logic
type Update = Msg -> Model -> Model * Cmd<Msg>  // alias for readability
let update msg model =
    match msg with
    | UpdateDescription d -> { model with DescriptionInput = d }, Cmd.none
    | UpdateAmount a -> { model with AmountInput = a }, Cmd.none
    | UpdateType t -> { model with TypeInput = t }, Cmd.none
    | AddTransaction ->
        match Double.TryParse model.AmountInput with
        | true, amt when model.DescriptionInput.Trim() <> "" ->
            let tx = { Id = Guid.NewGuid(); Date = DateTime.Now; Description = model.DescriptionInput; Amount = amt; Type = model.TypeInput }
            { model with Transactions = tx :: model.Transactions; DescriptionInput = ""; AmountInput = "" }, Cmd.none
        | _ -> model, Cmd.none
    | DeleteTransaction id -> { model with Transactions = model.Transactions |> List.filter (fun x -> x.Id <> id) }, Cmd.none

/// View
let view model dispatch =
    div [] [
        h2 [] [ str "BudgetBuddy" ]
        div [] [
            input [ Type "text"; Placeholder "Description"; Value model.DescriptionInput; OnChange (fun e -> dispatch (UpdateDescription e.Value)) ]
            input [ Type "number"; Placeholder "Amount"; Value model.AmountInput; OnChange (fun e -> dispatch (UpdateAmount e.Value)) ]
            select [ Value (if model.TypeInput = Expense then "Expense" else "Income"); OnChange (fun e -> dispatch (UpdateType (if e.Value="Expense" then Expense else Income))) ] [
                option [ Value "Expense" ] [ str "Expense" ]
                option [ Value "Income" ]  [ str "Income" ]
            ]
            button [ OnClick (fun _ -> dispatch AddTransaction) ] [ str "Add" ]
        ]
        ul [] (
            model.Transactions |> List.map (fun tx ->
                li [] [
                    str (sprintf "%s %s %s%.2f" (tx.Date.ToShortDateString()) tx.Description (if tx.Type=Expense then "-" else "+") tx.Amount)
                    button [ OnClick (fun _ -> dispatch (DeleteTransaction tx.Id)) ] [ str "Delete" ]
                ]))
    ]

// Entry point: wire Elmish with React
Program.mkProgram init update view
|> Program.withReactSynchronous "elmish-app"
|> Program.run