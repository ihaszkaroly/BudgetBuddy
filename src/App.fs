module App

open System
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Browser.Dom
open Fable.Core

// JSON helpers
[<Emit("JSON.stringify($0)")>]
let jsonStringify (o: obj): string = jsNative
[<Emit("JSON.parse($0)")>]
let jsonParse (s: string): obj = jsNative

let storageKey = "transactions"

// Domain discriminated unions
type TransactionType = Expense | Income
type Category      = Food | Transportation | Utilities | Entertainment | Other
type Filter        = All | IncomeFilter | CategoryFilter of Category

// Domain model
type Transaction = {
    Id          : Guid
    Date        : DateTime
    Description : string
    Amount      : float
    Type        : TransactionType
    Category    : Category
}

// DTO for persistence
type RawTransaction = {
    Id          : string
    Date        : string
    Description : string
    Amount      : float
    Type        : string
    Category    : string
}

// Convert from DTO to domain
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

// Convert from domain to DTO
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

// Load and save
let loadTransactions (): Transaction list =
    match window.localStorage.getItem(storageKey) with
    | null | "" -> []
    | str ->
        let rawArr = jsonParse str |> unbox<RawTransaction[]>
        rawArr |> Array.toList |> List.map toTransaction

let saveTransactions (txs: Transaction list) =
    let rawArr = txs |> List.map fromTransaction |> List.toArray
    window.localStorage.setItem(storageKey, jsonStringify (box rawArr))

// Elmish model and messages
type Model = {
    Transactions     : Transaction list
    DescriptionInput : string
    AmountInput      : string
    TypeInput        : TransactionType
    CategoryInput    : Category
    Filter           : Filter
}

type Msg =
    | UpdateDescription  of string
    | UpdateAmount       of string
    | UpdateType         of TransactionType
    | UpdateCategory     of Category
    | UpdateFilter       of Filter
    | AddTransaction
    | DeleteTransaction  of Guid

// Initialize
let init (): Model * Cmd<Msg> =
    { Transactions     = loadTransactions()
      DescriptionInput = ""
      AmountInput      = ""
      TypeInput        = Expense
      CategoryInput    = Other
      Filter           = All },
    Cmd.none

// Update
let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg with
    | UpdateDescription d -> { model with DescriptionInput = d }, Cmd.none
    | UpdateAmount a      -> { model with AmountInput = a }, Cmd.none
    | UpdateType t        -> { model with TypeInput = t; CategoryInput = Other }, Cmd.none
    | UpdateCategory c    -> { model with CategoryInput = c }, Cmd.none
    | UpdateFilter f      -> { model with Filter = f }, Cmd.none

    | AddTransaction ->
        match Double.TryParse model.AmountInput with
        | true, amt when amt >= 0.0 && not (String.IsNullOrWhiteSpace model.DescriptionInput) ->
            let tx: Transaction = {
                Id          = Guid.NewGuid()
                Date        = DateTime.Now
                Description = model.DescriptionInput
                Amount      = amt
                Type        = model.TypeInput
                Category    = match model.TypeInput with Income -> Other | Expense -> model.CategoryInput
            }
            let updated = tx :: model.Transactions
            saveTransactions updated
            { model with Transactions = updated; DescriptionInput = ""; AmountInput = "" }, Cmd.none
        | _ ->
            model, Cmd.none

    | DeleteTransaction id ->
        let updated = model.Transactions |> List.filter (fun t -> t.Id <> id)
        saveTransactions updated
        { model with Transactions = updated }, Cmd.none

// View
let view (model: Model) (dispatch: Msg -> unit) =
    // Summary
    let totalIncome = model.Transactions |> List.filter (fun t -> t.Type = Income) |> List.sumBy (fun t -> t.Amount)
    let totalExpenses = model.Transactions |> List.filter (fun t -> t.Type = Expense) |> List.sumBy (fun t -> t.Amount)
    let balance = totalIncome - totalExpenses

    // Conditional color
    let balanceColor =
        if balance < 0.0 then "red"
        elif balance > 100.0 then "green"
        else "black"

    // Filtered
    let displayedTxs =
        model.Transactions
        |> List.filter (fun t ->
            match model.Filter with
            | All              -> true
            | IncomeFilter     -> t.Type = Income
            | CategoryFilter c -> t.Type = Expense && t.Category = c)

    // Chart data
    let expenseCats = [Food; Transportation; Utilities; Entertainment; Other]
    let totals =
        expenseCats
        |> List.map (fun c ->
            let sumC =
                model.Transactions
                |> List.filter (fun t -> t.Type = Expense && t.Category = c)
                |> List.sumBy (fun t -> t.Amount)
            c, sumC)
    let maxTotal = match totals |> List.map snd with [] -> 1.0 | xs -> List.max xs

    // Bars
    let barElements =
        totals
        |> List.mapi (fun _ (cat, amt) ->
            let height = if maxTotal > 0.0 then amt / maxTotal * 150.0 else 0.0
            let label =
                match cat with
                | Food           -> "Food"
                | Transportation -> "Transport"
                | Utilities      -> "Utilities"
                | Entertainment  -> "Entertainment"
                | Other          -> "Other"
            div [ Style [
                    CSSProp.Custom("display","flex")
                    CSSProp.Custom("flex-direction","column")
                    CSSProp.Custom("align-items","center")
                    CSSProp.Custom("margin-right","20px")
                  ] ] [
                div [ Style [
                        CSSProp.Custom("width","60px")
                        CSSProp.Custom("height", sprintf "%.0fpx" height)
                        CSSProp.Custom("background-color","#007bff")
                      ] ] []
                div [ Style [
                        CSSProp.Custom("margin-top","4px")
                        CSSProp.Custom("font-size","12px")
                      ] ] [ str label ]
            ])

    div [] [
        // Main container
        div [ Style [
                CSSProp.Custom("margin","2rem auto")
                CSSProp.Custom("padding","1rem")
                CSSProp.Custom("max-width","600px")
                CSSProp.Custom("border","1px solid #ccc")
                CSSProp.Custom("border-radius","8px")
                CSSProp.Custom("box-shadow","0 2px 8px rgba(0,0,0,0.1)")
            ] ] [
            h2 [ Style [
                    CSSProp.Custom("margin-bottom","1rem")
                    CSSProp.Custom("font-family","Arial, sans-serif")
                ] ] [ str "BudgetBuddy" ]

            // Stats with conditional balance color
            div [ Style [
                    CSSProp.Custom("display","flex")
                    CSSProp.Custom("justify-content","space-between")
                    CSSProp.Custom("margin-bottom","1.5rem")
                ] ] [
                span [] [ str (sprintf "Total Income: €%.2f" totalIncome) ]
                span [] [ str (sprintf "Total Expenses: €%.2f" totalExpenses) ]
                span [ Style [ CSSProp.Custom("color", balanceColor) ] ]
                     [ str (sprintf "Balance: €%.2f" balance) ]
            ]

            // Input row
            div [ Style [
                    CSSProp.Custom("display", "flex")
                    CSSProp.Custom("flex-wrap", "wrap")
                    CSSProp.Custom("margin-bottom", "1rem")
                ] ] [
                input [
                    Type "text"
                    Placeholder "Description"
                    Value model.DescriptionInput
                    OnChange (fun e -> dispatch (UpdateDescription e.Value))
                    Style [
                        CSSProp.Custom("padding","0.5rem")
                        CSSProp.Custom("margin","0 0.5rem 0.5rem 0")
                        CSSProp.Custom("flex-grow","1")
                        CSSProp.Custom("border","1px solid #ccc")
                        CSSProp.Custom("border-radius","4px")
                    ]
                ]
                input [
                    Type "number"
                    Min "0"
                    Placeholder "Amount (€)"
                    Value model.AmountInput
                    OnChange (fun e -> dispatch (UpdateAmount e.Value))
                    Style [
                        CSSProp.Custom("padding","0.5rem")
                        CSSProp.Custom("margin","0 0.5rem 0.5rem 0")
                        CSSProp.Custom("width","100px")
                        CSSProp.Custom("border","1px solid #ccc")
                        CSSProp.Custom("border-radius","4px")
                    ]
                ]
                select [
                    Value (if model.TypeInput = Expense then "Expense" else "Income")
                    OnChange (fun e -> dispatch (UpdateType (if e.Value="Expense" then Expense else Income)))
                    Style [
                        CSSProp.Custom("padding","0.5rem")
                        CSSProp.Custom("margin","0 0.5rem 0.5rem 0")
                        CSSProp.Custom("border","1px solid #ccc")
                        CSSProp.Custom("border-radius","4px")
                    ]
                ] [
                    option [ Value "Expense" ] [ str "Expense" ]
                    option [ Value "Income"  ] [ str "Income"  ]
                ]
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
                        Style [
                            CSSProp.Custom("padding","0.5rem")
                            CSSProp.Custom("margin","0 0.5rem 0.5rem 0")
                            CSSProp.Custom("border","1px solid #ccc")
                            CSSProp.Custom("border-radius","4px")
                        ]
                    ] [
                        option [ Value "Food"           ] [ str "Food" ]
                        option [ Value "Transportation" ] [ str "Transportation" ]
                        option [ Value "Utilities"      ] [ str "Utilities" ]
                        option [ Value "Entertainment"  ] [ str "Entertainment" ]
                        option [ Value "Other"          ] [ str "Other" ]
                    ]
                button [
                    OnClick(fun _ -> dispatch AddTransaction)
                    Style [
                        CSSProp.Custom("padding","0.5rem 1rem")
                        CSSProp.Custom("background-color","#007bff")
                        CSSProp.Custom("color","white")
                        CSSProp.Custom("border","none")
                        CSSProp.Custom("border-radius","4px")
                        CSSProp.Custom("cursor","pointer")
                    ]
                ] [ str "Add" ]
            ]

            // Filter
            div [ Style [ CSSProp.Custom("margin-bottom","1rem") ] ] [
                str "Filter by: "
                select [
                    Value (
                        match model.Filter with
                        | All              -> ""
                        | IncomeFilter     -> "Income"
                        | CategoryFilter c ->
                            match c with
                            | Food           -> "Food"
                            | Transportation -> "Transportation"
                            | Utilities      -> "Utilities"
                            | Entertainment  -> "Entertainment"
                            | Other          -> "Other")
                    OnChange(fun e ->
                        let f =
                            match e.Value with
                            | ""             -> All
                            | "Income"       -> IncomeFilter
                            | "Food"         -> CategoryFilter Food
                            | "Transportation" -> CategoryFilter Transportation
                            | "Utilities"    -> CategoryFilter Utilities
                            | "Entertainment"-> CategoryFilter Entertainment
                            | _              -> CategoryFilter Other
                        dispatch (UpdateFilter f))
                    Style [
                        CSSProp.Custom("padding","0.5rem")
                        CSSProp.Custom("border","1px solid #ccc")
                        CSSProp.Custom("border-radius","4px")
                    ]
                ] [
                    option [ Value ""       ] [ str "All" ]
                    option [ Value "Income" ] [ str "Income" ]
                    option [ Value "Food"           ] [ str "Food" ]
                    option [ Value "Transportation" ] [ str "Transportation" ]
                    option [ Value "Utilities"      ] [ str "Utilities" ]
                    option [ Value "Entertainment"  ] [ str "Entertainment" ]
                    option [ Value "Other"          ] [ str "Other" ]
                ]
            ]

            // Transaction list
            ul [ Style [ CSSProp.Custom("list-style","none"); CSSProp.Custom("padding","0") ] ] (
                displayedTxs
                |> List.map (fun tx ->
                    li [ Style [
                            CSSProp.Custom("display","flex")
                            CSSProp.Custom("justify-content","space-between")
                            CSSProp.Custom("align-items","center")
                            CSSProp.Custom("padding","0.5rem 0")
                            CSSProp.Custom("border-bottom","1px solid #eee")
                          ] ] [
                        let sign   = if tx.Type = Expense then "-" else "+"
                        let catLabel =
                            if tx.Type = Expense then sprintf " [%s]" (
                                match tx.Category with
                                | Food           -> "Food"
                                | Transportation -> "Transportation"
                                | Utilities      -> "Utilities"
                                | Entertainment  -> "Entertainment"
                                | Other          -> "Other")
                            else ""
                        span [] [
                            str (sprintf "%s - %s%s: %s€%.2f"
                                    (tx.Date.ToShortDateString())
                                    tx.Description
                                    catLabel
                                    sign
                                    tx.Amount)
                        ]
                        button [
                            OnClick(fun _ -> dispatch (DeleteTransaction tx.Id))
                            Style [
                                CSSProp.Custom("padding","0.25rem 0.5rem")
                                CSSProp.Custom("background-color","#dc3545")
                                CSSProp.Custom("color","white")
                                CSSProp.Custom("border","none")
                                CSSProp.Custom("border-radius","4px")
                                CSSProp.Custom("cursor","pointer")
                            ]
                        ] [ str "Delete" ]
                    ]))
        ]

        // Column chart
        div [ Style [
                CSSProp.Custom("margin","2rem auto")
                CSSProp.Custom("text-align","center")
                CSSProp.Custom("width","100%")
            ] ] [
            h3 [] [ str "Expenses by Category" ]
            div [ Style [
                    CSSProp.Custom("display","flex")
                    CSSProp.Custom("justify-content","center")
                    CSSProp.Custom("align-items","flex-end")
                    CSSProp.Custom("height","180px")
                ] ] barElements
        ]
    ]

Program.mkProgram init update view
|> Program.withReactBatched "elmish-app"
|> Program.run