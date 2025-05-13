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
    // Calculate totals
    let totalExpenses =
        model.Transactions
        |> List.filter (fun t -> t.Type = Expense)
        |> List.sumBy (fun t -> t.Amount)
    let totalIncome =
        model.Transactions
        |> List.filter (fun t -> t.Type = Income)
        |> List.sumBy (fun t -> t.Amount)
    let balance = totalIncome - totalExpenses

    div [
        Style [
            // Container styling
            CSSProp.MaxWidth "600px"
            CSSProp.Margin "2rem auto"
            CSSProp.Padding "1rem"
            CSSProp.Border "1px solid #ccc"
            CSSProp.BorderRadius "8px"
            CSSProp.BoxShadow "0 2px 8px rgba(0,0,0,0.1)"
        ]
    ] [
        // Title
        h2 [ Style [ CSSProp.MarginBottom "1rem"; CSSProp.FontFamily "Arial, sans-serif" ] ] [ str "BudgetBuddy" ]

        // Summary statistics
        div [ Style [ CSSProp.MarginBottom "1.5rem"; CSSProp.Custom("display", "flex"); CSSProp.Custom("justify-content", "space-between") ] ] [
            span [] [ str (sprintf "Total Income: €%.2f" totalIncome) ]
            span [] [ str (sprintf "Total Expenses: €%.2f" totalExpenses) ]
            span [] [ str (sprintf "Balance: €%.2f" balance) ]
        ]

        // Input area
        div [ Style [ CSSProp.Custom("display", "flex"); CSSProp.MarginBottom "1rem" ] ] [
            input [
                Type "text"
                Placeholder "Description"
                Value model.DescriptionInput
                OnChange (fun e -> dispatch (UpdateDescription e.Value))
                Style [ CSSProp.Padding "0.5rem"; CSSProp.MarginRight "0.5rem"; CSSProp.FlexGrow "1"; CSSProp.Border "1px solid #ccc"; CSSProp.BorderRadius "4px" ]
            ]
            input [
                Type "number"
                Placeholder "Amount (in €)"
                Value model.AmountInput
                OnChange (fun e -> dispatch (UpdateAmount e.Value))
                Style [ CSSProp.Padding "0.5rem"; CSSProp.MarginRight "0.5rem"; CSSProp.Width "100px"; CSSProp.Border "1px solid #ccc"; CSSProp.BorderRadius "4px" ]
            ]
            select [
                Value (if model.TypeInput = Expense then "Expense" else "Income")
                OnChange (fun e -> dispatch (UpdateType (if e.Value = "Expense" then Expense else Income)))
                Style [ CSSProp.Padding "0.5rem"; CSSProp.MarginRight "0.5rem"; CSSProp.Border "1px solid #ccc"; CSSProp.BorderRadius "4px" ]
            ] [
                option [ Value "Expense" ] [ str "Expense" ]
                option [ Value "Income"  ] [ str "Income"  ]
            ]
            button [
                OnClick (fun _ -> dispatch AddTransaction)
                Style [ CSSProp.Padding "0.5rem 1rem"; CSSProp.BackgroundColor "#007bff"; CSSProp.Color "white"; CSSProp.Border "none"; CSSProp.BorderRadius "4px"; CSSProp.Cursor "pointer" ]
            ] [ str "Add" ]
        ]

        // Transaction list
        ul [ Style [ CSSProp.ListStyleType "none"; CSSProp.Padding "0" ] ] (
            model.Transactions
            |> List.map (fun tx ->
                li [ Style [ CSSProp.Custom("display", "flex"); CSSProp.Custom("justify-content", "space-between"); CSSProp.Custom("align-items", "center"); CSSProp.Padding "0.5rem 0"; CSSProp.BorderBottom "1px solid #eee" ] ] [
                    span [] [ str (sprintf "%s - %s: %s€%.2f" (tx.Date.ToShortDateString()) tx.Description (if tx.Type = Expense then "-" else "+") tx.Amount) ]
                    button [
                        OnClick (fun _ -> dispatch (DeleteTransaction tx.Id))
                        Style [ CSSProp.Padding "0.25rem 0.5rem"; CSSProp.BackgroundColor "#dc3545"; CSSProp.Color "white"; CSSProp.Border "none"; CSSProp.BorderRadius "4px"; CSSProp.Cursor "pointer" ]
                    ] [ str "Delete" ]
                ]))
    ]

Program.mkProgram init update view
|> Program.withReactBatched "elmish-app"
|> Program.run
