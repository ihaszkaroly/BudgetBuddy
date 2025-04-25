module BudgetBuddy.App

open Elmish
open Elmish.React
open Browser.Dom
open BudgetBuddy.Model
open BudgetBuddy.Update
open BudgetBuddy.View

Program.mkProgram init update view
|> Program.withReactBatched "root"
|> Program.run
