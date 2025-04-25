module BudgetBuddy.View

open Fable.React
open Fable.React.Props
open BudgetBuddy.Model
open BudgetBuddy.Update

let view model dispatch =
    div [] [
        h1 [ Class "text-2xl font-bold mb-4" ] [ str "💸 BudgetBuddy" ]

        ul [] (
            model.Transactions
            |> List.map (fun t ->
                li [ Key (string t.Id) ] [
                    str $"{t.Description} - {t.Amount} ({t.Category})"
                    button [
                        OnClick (fun _ -> dispatch (RemoveTransaction t.Id))
                        Class "ml-4 text-red-500"
                    ] [ str "✕" ]
                ]
            )
        )
    ]
