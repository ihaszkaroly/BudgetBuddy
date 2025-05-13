module App

open System
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Browser.Dom
open Fable.Core

[<Emit("JSON.stringify($0)")>]
let jsonStringify (o: obj): string = jsNative

[<Emit("JSON.parse($0)")>]
let jsonParse (s: string): obj = jsNative

let storageKey = "transactions"

// Domain types

type TransactionType =
    | Expense
    | Income


type Transaction = {
    Id: Guid
    Date: DateTime
    Description: string
    Amount: float
    Type: TransactionType
}


type Model = {
    Transactions: Transaction list
    DescriptionInput: string
    AmountInput: string
    TypeInput: TransactionType
}

type Msg =
    | UpdateDescription of string
    | UpdateAmount of string
    | UpdateType of TransactionType
    | AddTransaction
    | DeleteTransaction of Guid

// Persistence helpers

type RawTransaction = {
    Id: string
    Date: string
    Description: string
    Amount: float
    Type: string
}

// Convert raw DTO to domain
let toTransaction (raw: RawTransaction) : Transaction =
    { Id = Guid.Parse raw.Id
      Date = DateTime.Parse raw.Date
      Description = raw.Description
      Amount = raw.Amount
      Type = if raw.Type = "Expense" then Expense else Income }

// Convert domain to raw DTO
let fromTransaction (tx: Transaction) : RawTransaction =
    { Id = tx.Id.ToString()
      Date = tx.Date.ToString("o")
      Description = tx.Description
      Amount = tx.Amount
      Type = if tx.Type = Expense then "Expense" else "Income" }

let loadTransactions (): Transaction list =
    match window.localStorage.getItem(storageKey) with
    | null | "" -> []
    | str ->
        let rawArr: RawTransaction[] =
            jsonParse str
            |> unbox<RawTransaction[]>
        rawArr
        |> Array.toList
        |> List.map toTransaction

let saveTransactions (txs: Transaction list) =
    // Convert to raw representation with ISO date strings
    let rawArr =
        txs
        |> List.map fromTransaction
        |> Array.ofList
    window.localStorage.setItem(storageKey, jsonStringify (box rawArr))

// Initialize

let init (): Model * Cmd<Msg> =
    { Transactions = loadTransactions()
      DescriptionInput = ""
      AmountInput = ""
      TypeInput = Expense },
    Cmd.none

// Update logic

let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg with
    | UpdateDescription d ->
        { model with DescriptionInput = d }, Cmd.none

    | UpdateAmount a ->
        { model with AmountInput = a }, Cmd.none

    | UpdateType t ->
        { model with TypeInput = t }, Cmd.none

    | AddTransaction ->
        match System.Double.TryParse model.AmountInput with
        | true, amt when model.DescriptionInput.Trim() <> "" ->
            // Create a new Transaction record
            let tx: Transaction = {
                Id = Guid.NewGuid()
                Date = DateTime.Now
                Description = model.DescriptionInput
                Amount = amt
                Type = model.TypeInput
            }
            let updated = tx :: model.Transactions
            // Persist updated list
            saveTransactions updated
            { model with Transactions = updated; DescriptionInput = ""; AmountInput = "" }, Cmd.none
        | _ -> model, Cmd.none

    | DeleteTransaction id ->
        let updated = model.Transactions |> List.filter (fun t -> t.Id <> id)
        saveTransactions updated
        { model with Transactions = updated }, Cmd.none

// View

let view (model: Model) (dispatch: Msg -> unit) =
    div [] [
        h2 [] [ str "BudgetBuddy" ]

        div [] [
            input [
                Type "text"
                Placeholder "Description"
                Value model.DescriptionInput
                OnChange (fun e -> dispatch (UpdateDescription e.Value))
            ]
            input [
                Type "number"
                Placeholder "Amount"
                Value model.AmountInput
                OnChange (fun e -> dispatch (UpdateAmount e.Value))
            ]
            select [
                Value (if model.TypeInput = Expense then "Expense" else "Income")
                OnChange (fun e ->
                    let t = if e.Value = "Expense" then Expense else Income
                    dispatch (UpdateType t))
            ] [
                option [ Value "Expense" ] [ str "Expense" ]
                option [ Value "Income"  ] [ str "Income"  ]
            ]
            button [ OnClick (fun _ -> dispatch AddTransaction) ] [ str "Add" ]
        ]

        ul [] (
            model.Transactions
            |> List.map (fun tx ->
                li [] [
                    str (sprintf "%s - %s: %s€%.2f"
                        (tx.Date.ToShortDateString())
                        tx.Description
                        (if tx.Type = Expense then "-" else "+")
                        tx.Amount)
                    button [ OnClick (fun _ -> dispatch (DeleteTransaction tx.Id)) ] [ str "Delete" ]
                ])
        )
    ]

// Elmish startup

Program.mkProgram init update view
|> Program.withReactBatched "elmish-app"
|> Program.run
