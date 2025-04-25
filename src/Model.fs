module BudgetBuddy.Model

type Category =
    | Food
    | Rent
    | Utilities
    | Transportation
    | Entertainment
    | Salary
    | Other of string

type TransactionType =
    | Income
    | Expense

type Transaction = {
    Id: int
    Description: string
    Amount: float
    Date: System.DateTime
    Category: Category
    Kind: TransactionType
}

type Model = {
    Transactions: Transaction list
    NextId: int
}
