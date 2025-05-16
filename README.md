# BudgetBuddy

A lightweight personal budgeting web app written in F# with Fable and Elmish. Track income and expenses in real time through a clean, responsive interface.

## Motivation

Personal finance can get messy without clear visibility. BudgetBuddy helps you:
- See your current balance at a glance
- Quickly log income and expenses
- Categorize transactions for better insights
- Run entirely in your browser (no backend required)

## Live Demo

[Click here to run the app.](https://ihaszkaroly.github.io/BudgetBuddy/)

## Screenshots

![kép](https://github.com/user-attachments/assets/bb2e6b10-d248-4741-8e92-81e41ceebe87)

## Getting Started

### Prerequisites

- [.NET 6.0+ SDK](https://dotnet.microsoft.com/download)  
- [Node.js 14+](https://nodejs.org/) (npm included)

### Installation & Run

```bash
git clone https://github.com/ihaszkaroly/BudgetBuddy.git
cd BudgetBuddy
npm install
npm run build     # compiles F# to JS and bundles with Webpack
npm start         # starts dev server at http://localhost:8080
