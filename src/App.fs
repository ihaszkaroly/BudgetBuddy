module App

open Elmish.React
open Elmish
open Fable.React
open Feliz
open Feliz.Bulma
open System

type Expense = {
    Id: Guid
    Description: string
    Amount: float
}

type Model = {
    Expenses: Expense list
    NewDescription: string
    NewAmount: string
}

type Msg =
    | UpdateDescription of string
    | UpdateAmount of string
    | AddExpense
    | DeleteExpense of Guid

let init () =
    {
        Expenses = []
        NewDescription = ""
        NewAmount = ""
    }, Cmd.none

let update msg model =
    match msg with
    | UpdateDescription desc -> { model with NewDescription = desc }, Cmd.none
    | UpdateAmount amt -> { model with NewAmount = amt }, Cmd.none
    | AddExpense ->
        match System.Double.TryParse(model.NewAmount) with
        | true, value ->
            let newExp = {
                Id = Guid.NewGuid()
                Description = model.NewDescription
                Amount = value
            }
            let newModel = {
                model with
                    Expenses = newExp :: model.Expenses
                    NewDescription = ""
                    NewAmount = ""
            }
            newModel, Cmd.none
        | _ -> model, Cmd.none
    | DeleteExpense id ->
        let updated = model.Expenses |> List.filter (fun e -> e.Id <> id)
        { model with Expenses = updated }, Cmd.none

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        Html.h1 [ prop.text "💸 BudgetBuddy" ]

        Html.div [
            Html.input [
                prop.placeholder "Description"
                prop.value model.NewDescription
                prop.onChange (UpdateDescription >> dispatch)
            ]
            Html.input [
                prop.placeholder "Amount"
                prop.value model.NewAmount
                prop.custom ("inputMode", "decimal")
                prop.onChange (UpdateAmount >> dispatch)
            ]
            Html.button [
                prop.text "Add"
                prop.onClick (fun _ -> dispatch AddExpense)
            ]
        ]

        Html.ul [
            for exp in model.Expenses do
                Html.li [
                    Html.span [ prop.text $"{exp.Description}: ${exp.Amount}" ]
                    Html.button [
                        prop.text "❌"
                        prop.onClick (fun _ -> dispatch (DeleteExpense exp.Id))
                    ]
                ]
        ]
    ]

Program.mkProgram init update view
|> Program.withReactBatched "root"
|> Program.run
