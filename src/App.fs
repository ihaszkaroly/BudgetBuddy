module App

open System
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Browser.Dom
open Fable.Core

// JSON-helpers
[<Emit("JSON.stringify($0)")>]
let jsonStringify (o: obj): string = jsNative
[<Emit("JSON.parse($0)")>]
let jsonParse (s: string): obj = jsNative

let storageKey = "transactions"

// DU-k a domainben
type TransactionType = Expense | Income
type Category = Food | Transportation | Utilities | Entertainment | Other

// Domain Transaction
type Transaction = {
    Id: Guid
    Date: DateTime
    Description: string
    Amount: float
    Type: TransactionType
    Category: Category
}

// DTO a localStorage-hoz
type RawTransaction = {
    Id: string
    Date: string
    Description: string
    Amount: float
    Type: string
    Category: string
}

// DTO -> Domain
let toTransaction (raw: RawTransaction): Transaction =
    let txType =
        match raw.Type with
        | "Expense" -> Expense
        | "Income"  -> Income
        | _         -> Income
    let cat =
        match raw.Category with
        | "Food"           -> Food
        | "Transportation" -> Transportation
        | "Utilities"      -> Utilities
        | "Entertainment"  -> Entertainment
        | _                -> Other
    { Id          = Guid.Parse raw.Id
      Date        = DateTime.Parse raw.Date
      Description = raw.Description
      Amount      = raw.Amount
      Type        = txType
      Category    = cat }

// Domain -> DTO
let fromTransaction (tx: Transaction): RawTransaction =
    let txType = if tx.Type = Expense then "Expense" else "Income"
    let cat =
        match tx.Category with
        | Food           -> "Food"
        | Transportation -> "Transportation"
        | Utilities      -> "Utilities"
        | Entertainment  -> "Entertainment"
        | Other          -> "Other"
    { Id          = tx.Id.ToString()
      Date        = tx.Date.ToString("o")
      Description = tx.Description
      Amount      = tx.Amount
      Type        = txType
      Category    = cat }

// Betöltés localStorage-ból
let loadTransactions (): Transaction list =
    match window.localStorage.getItem(storageKey) with
    | null | "" -> []
    | str ->
        let rawArr = jsonParse str |> unbox<RawTransaction[]>
        rawArr |> Array.toList |> List.map toTransaction

// Mentés localStorage-ba
let saveTransactions (txs: Transaction list) =
    let rawArr = txs |> List.map fromTransaction |> List.toArray
    window.localStorage.setItem(storageKey, jsonStringify (box rawArr))

// Elmish model és üzenetek
type Model = {
    Transactions     : Transaction list
    DescriptionInput : string
    AmountInput      : string
    TypeInput        : TransactionType
    CategoryInput    : Category
}

type Msg =
    | UpdateDescription of string
    | UpdateAmount      of string
    | UpdateType        of TransactionType
    | UpdateCategory    of Category
    | AddTransaction
    | DeleteTransaction of Guid

// Inicializálás
let init (): Model * Cmd<Msg> =
    { Transactions     = loadTransactions()
      DescriptionInput = ""
      AmountInput      = ""
      TypeInput        = Expense
      CategoryInput    = Other },
    Cmd.none

// Update
let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg with
    | UpdateDescription d ->
        { model with DescriptionInput = d }, Cmd.none

    | UpdateAmount a ->
        { model with AmountInput = a }, Cmd.none

    | UpdateType t ->
        // típusváltáskor reseteljük a kategóriát
        { model with TypeInput = t; CategoryInput = Other }, Cmd.none

    | UpdateCategory c ->
        { model with CategoryInput = c }, Cmd.none

    | AddTransaction ->
        match Double.TryParse model.AmountInput with
        | true, amt when not (String.IsNullOrWhiteSpace model.DescriptionInput) ->
            // Itt építünk DOMAIN Transaction-t, NEM RawTransaction-t
            let tx: Transaction = {
                Id          = Guid.NewGuid()
                Date        = DateTime.Now
                Description = model.DescriptionInput
                Amount      = amt
                Type        = model.TypeInput
                Category    = model.CategoryInput
            }
            let updated = tx :: model.Transactions
            saveTransactions updated
            { model with
                Transactions     = updated
                DescriptionInput = ""
                AmountInput      = "" },
            Cmd.none
        | _ ->
            model, Cmd.none

    | DeleteTransaction id ->
        let updated = model.Transactions |> List.filter (fun t -> t.Id <> id)
        saveTransactions updated
        { model with Transactions = updated }, Cmd.none

// View
let view (model: Model) (dispatch: Msg -> unit) =
    let totalIncome   = model.Transactions |> List.filter (fun t -> t.Type = Income)  |> List.sumBy (fun t -> t.Amount)
    let totalExpenses = model.Transactions |> List.filter (fun t -> t.Type = Expense) |> List.sumBy (fun t -> t.Amount)
    let balance       = totalIncome - totalExpenses

    div [ Style [
            CSSProp.MaxWidth      "600px"
            CSSProp.Margin        "2rem auto"
            CSSProp.Padding       "1rem"
            CSSProp.Border        "1px solid #ccc"
            CSSProp.BorderRadius  "8px"
            CSSProp.BoxShadow     "0 2px 8px rgba(0,0,0,0.1)"
        ] ] [
        h2 [ Style [ CSSProp.MarginBottom "1rem"; CSSProp.FontFamily "Arial, sans-serif" ] ] [ str "BudgetBuddy" ]

        // Összefoglaló
        div [ Style [
                CSSProp.Custom("display","flex")
                CSSProp.Custom("justify-content","space-between")
                CSSProp.MarginBottom "1.5rem"
            ] ] [
            span [] [ str (sprintf "Total Income: €%.2f" totalIncome) ]
            span [] [ str (sprintf "Total Expenses: €%.2f" totalExpenses) ]
            span [] [ str (sprintf "Balance: €%.2f" balance) ]
        ]

        // Beviteli rész
        div [ Style [
                CSSProp.Custom("display","flex")
                CSSProp.Custom("flex-wrap","wrap")
                CSSProp.MarginBottom "1rem"
            ] ] [
            input [
                Type "text"
                Placeholder "Description"
                Value model.DescriptionInput
                OnChange (fun e -> dispatch (UpdateDescription e.Value))
                Style [ CSSProp.Padding "0.5rem"; CSSProp.Margin "0 0.5rem 0.5rem 0"; CSSProp.FlexGrow "1"; CSSProp.Border "1px solid #ccc"; CSSProp.BorderRadius "4px" ]
            ]
            input [
                Type "number"
                Placeholder "Amount (€)"
                Value model.AmountInput
                OnChange (fun e -> dispatch (UpdateAmount e.Value))
                Style [ CSSProp.Padding "0.5rem"; CSSProp.Margin "0 0.5rem 0.5rem 0"; CSSProp.Width "100px"; CSSProp.Border "1px solid #ccc"; CSSProp.BorderRadius "4px" ]
            ]
            select [
                Value (if model.TypeInput = Expense then "Expense" else "Income")
                OnChange (fun e -> dispatch (UpdateType (if e.Value = "Expense" then Expense else Income)))
                Style [ CSSProp.Padding "0.5rem"; CSSProp.Margin "0 0.5rem 0.5rem 0"; CSSProp.Border "1px solid #ccc"; CSSProp.BorderRadius "4px" ]
            ] [
                option [ Value "Expense" ] [ str "Expense" ]
                option [ Value "Income"  ] [ str "Income"  ]
            ]

            // Kategória csak Expense esetén
            if model.TypeInput = Expense then
                select [
                    Value (
                        match model.CategoryInput with
                        | Food           -> "Food"
                        | Transportation -> "Transportation"
                        | Utilities      -> "Utilities"
                        | Entertainment  -> "Entertainment"
                        | Other          -> "Other")
                    OnChange (fun e ->
                        let c =
                            match e.Value with
                            | "Food"           -> Food
                            | "Transportation" -> Transportation
                            | "Utilities"      -> Utilities
                            | "Entertainment"  -> Entertainment
                            | _                -> Other
                        dispatch (UpdateCategory c))
                    Style [ CSSProp.Padding "0.5rem"; CSSProp.Margin "0 0.5rem 0.5rem 0"; CSSProp.Border "1px solid #ccc"; CSSProp.BorderRadius "4px" ]
                ] [
                    option [ Value "Food"           ] [ str "Food" ]
                    option [ Value "Transportation" ] [ str "Transportation" ]
                    option [ Value "Utilities"      ] [ str "Utilities" ]
                    option [ Value "Entertainment"  ] [ str "Entertainment" ]
                    option [ Value "Other"          ] [ str "Other" ]
                ]

            button [
                OnClick (fun _ -> dispatch AddTransaction)
                Style [ CSSProp.Padding "0.5rem 1rem"; CSSProp.BackgroundColor "#007bff"; CSSProp.Color "white"; CSSProp.Border "none"; CSSProp.BorderRadius "4px"; CSSProp.Cursor "pointer" ]
            ] [ str "Add" ]
        ]

        // Tranzakció lista
        ul [ Style [ CSSProp.ListStyleType "none"; CSSProp.Padding "0" ] ] (
            model.Transactions
            |> List.map (fun tx ->
                li [ Style [
                        CSSProp.Custom("display","flex")
                        CSSProp.Custom("justify-content","space-between")
                        CSSProp.Custom("align-items","center")
                        CSSProp.Padding "0.5rem 0"
                        CSSProp.BorderBottom "1px solid #eee"
                    ] ] [
                    let sign   = if tx.Type = Expense then "-" else "+"
                    let catStr = if tx.Type = Expense then sprintf " [%s]" (
                                        match tx.Category with
                                        | Food           -> "Food"
                                        | Transportation -> "Transportation"
                                        | Utilities      -> "Utilities"
                                        | Entertainment  -> "Entertainment"
                                        | Other          -> "Other")
                                 else ""
                    span [] [ str (sprintf "%s - %s%s: %s€%.2f"
                                        (tx.Date.ToShortDateString())
                                        tx.Description
                                        catStr
                                        sign
                                        tx.Amount) ]
                    button [
                        OnClick (fun _ -> dispatch (DeleteTransaction tx.Id))
                        Style [ CSSProp.Padding "0.25rem 0.5rem"; CSSProp.BackgroundColor "#dc3545"; CSSProp.Color "white"; CSSProp.Border "none"; CSSProp.BorderRadius "4px"; CSSProp.Cursor "pointer" ]
                    ] [ str "Delete" ]
                ]))
    ]

Program.mkProgram init update view
|> Program.withReactBatched "elmish-app"
|> Program.run
