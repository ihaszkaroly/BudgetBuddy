import { FSharpRef, Record, Union } from "./fable_modules/fable-library.3.7.20/Types.js";
import { list_type, record_type, float64_type, string_type, class_type, union_type } from "./fable_modules/fable-library.3.7.20/Reflection.js";
import { newGuid, parse } from "./fable_modules/fable-library.3.7.20/Guid.js";
import { toShortDateString, now, toString, parse as parse_1 } from "./fable_modules/fable-library.3.7.20/Date.js";
import { equals } from "./fable_modules/fable-library.3.7.20/Util.js";
import { sumBy, filter, cons, toArray, ofArray, map, empty } from "./fable_modules/fable-library.3.7.20/List.js";
import { Cmd_none } from "./fable_modules/Fable.Elmish.4.3.0/cmd.fs.js";
import { tryParse } from "./fable_modules/fable-library.3.7.20/Double.js";
import * as react from "react";
import { keyValueList } from "./fable_modules/fable-library.3.7.20/MapUtil.js";
import { printf, toText } from "./fable_modules/fable-library.3.7.20/String.js";
import { DOMAttr, HTMLAttr } from "./fable_modules/Fable.React.9.4.0/Fable.React.Props.fs.js";
import { ProgramModule_mkProgram, ProgramModule_run } from "./fable_modules/Fable.Elmish.4.3.0/program.fs.js";
import { Program_withReactBatched } from "./fable_modules/Fable.Elmish.React.4.0.0/react.fs.js";

export const storageKey = "transactions";

export class TransactionType extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["Expense", "Income"];
    }
}

export function TransactionType$reflection() {
    return union_type("App.TransactionType", [], TransactionType, () => [[], []]);
}

export class Transaction extends Record {
    constructor(Id, Date$, Description, Amount, Type) {
        super();
        this.Id = Id;
        this.Date = Date$;
        this.Description = Description;
        this.Amount = Amount;
        this.Type = Type;
    }
}

export function Transaction$reflection() {
    return record_type("App.Transaction", [], Transaction, () => [["Id", class_type("System.Guid")], ["Date", class_type("System.DateTime")], ["Description", string_type], ["Amount", float64_type], ["Type", TransactionType$reflection()]]);
}

export class Model extends Record {
    constructor(Transactions, DescriptionInput, AmountInput, TypeInput) {
        super();
        this.Transactions = Transactions;
        this.DescriptionInput = DescriptionInput;
        this.AmountInput = AmountInput;
        this.TypeInput = TypeInput;
    }
}

export function Model$reflection() {
    return record_type("App.Model", [], Model, () => [["Transactions", list_type(Transaction$reflection())], ["DescriptionInput", string_type], ["AmountInput", string_type], ["TypeInput", TransactionType$reflection()]]);
}

export class Msg extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["UpdateDescription", "UpdateAmount", "UpdateType", "AddTransaction", "DeleteTransaction"];
    }
}

export function Msg$reflection() {
    return union_type("App.Msg", [], Msg, () => [[["Item", string_type]], [["Item", string_type]], [["Item", TransactionType$reflection()]], [], [["Item", class_type("System.Guid")]]]);
}

export class RawTransaction extends Record {
    constructor(Id, Date$, Description, Amount, Type) {
        super();
        this.Id = Id;
        this.Date = Date$;
        this.Description = Description;
        this.Amount = Amount;
        this.Type = Type;
    }
}

export function RawTransaction$reflection() {
    return record_type("App.RawTransaction", [], RawTransaction, () => [["Id", string_type], ["Date", string_type], ["Description", string_type], ["Amount", float64_type], ["Type", string_type]]);
}

export function toTransaction(raw) {
    return new Transaction(parse(raw.Id), parse_1(raw.Date), raw.Description, raw.Amount, (raw.Type === "Expense") ? (new TransactionType(0)) : (new TransactionType(1)));
}

export function fromTransaction(tx) {
    return new RawTransaction(tx.Id, toString(tx.Date, "o"), tx.Description, tx.Amount, equals(tx.Type, new TransactionType(0)) ? "Expense" : "Income");
}

export function loadTransactions() {
    const matchValue = window.localStorage.getItem(storageKey);
    switch (matchValue) {
        case null:
        case "": {
            return empty();
        }
        default: {
            return map(toTransaction, ofArray(JSON.parse(matchValue)));
        }
    }
}

export function saveTransactions(txs) {
    const rawArr = toArray(map(fromTransaction, txs));
    window.localStorage.setItem(storageKey, JSON.stringify(rawArr));
}

export function init() {
    return [new Model(loadTransactions(), "", "", new TransactionType(0)), Cmd_none()];
}

export function update(msg, model) {
    switch (msg.tag) {
        case 1: {
            return [new Model(model.Transactions, model.DescriptionInput, msg.fields[0], model.TypeInput), Cmd_none()];
        }
        case 2: {
            return [new Model(model.Transactions, model.DescriptionInput, model.AmountInput, msg.fields[0]), Cmd_none()];
        }
        case 3: {
            let matchValue;
            let outArg = 0;
            matchValue = [tryParse(model.AmountInput, new FSharpRef(() => outArg, (v) => {
                outArg = v;
            })), outArg];
            let pattern_matching_result;
            if (matchValue[0]) {
                if (model.DescriptionInput.trim() !== "") {
                    pattern_matching_result = 0;
                }
                else {
                    pattern_matching_result = 1;
                }
            }
            else {
                pattern_matching_result = 1;
            }
            switch (pattern_matching_result) {
                case 0: {
                    const updated = cons(new Transaction(newGuid(), now(), model.DescriptionInput, matchValue[1], model.TypeInput), model.Transactions);
                    saveTransactions(updated);
                    return [new Model(updated, "", "", model.TypeInput), Cmd_none()];
                }
                case 1: {
                    return [model, Cmd_none()];
                }
            }
        }
        case 4: {
            const updated_1 = filter((t_1) => (t_1.Id !== msg.fields[0]), model.Transactions);
            saveTransactions(updated_1);
            return [new Model(updated_1, model.DescriptionInput, model.AmountInput, model.TypeInput), Cmd_none()];
        }
        default: {
            return [new Model(model.Transactions, msg.fields[0], model.AmountInput, model.TypeInput), Cmd_none()];
        }
    }
}

export function view(model, dispatch) {
    let props, props_8, children_8, children_2, children_4, children_6, props_22, children_18, props_10, props_12, props_18, children_14, props_20, props_30, children_26;
    const totalExpenses = sumBy((t_1) => t_1.Amount, filter((t) => equals(t.Type, new TransactionType(0)), model.Transactions), {
        GetZero: () => 0,
        Add: (x, y) => (x + y),
    });
    const totalIncome = sumBy((t_3) => t_3.Amount, filter((t_2) => equals(t_2.Type, new TransactionType(1)), model.Transactions), {
        GetZero: () => 0,
        Add: (x_1, y_1) => (x_1 + y_1),
    });
    const balance = totalIncome - totalExpenses;
    const props_32 = [["style", {
        maxWidth: "600px",
        margin: "2rem auto",
        padding: "1rem",
        border: "1px solid #ccc",
        borderRadius: "8px",
        boxShadow: "0 2px 8px rgba(0,0,0,0.1)",
    }]];
    const children_28 = [(props = [["style", {
        marginBottom: "1rem",
        fontFamily: "Arial, sans-serif",
    }]], react.createElement("h2", keyValueList(props, 1), "BudgetBuddy")), (props_8 = [["style", {
        marginBottom: "1.5rem",
        display: "flex",
        ["justify-content"]: "space-between",
    }]], (children_8 = [(children_2 = [toText(printf("Total Income: €%.2f"))(totalIncome)], react.createElement("span", {}, ...children_2)), (children_4 = [toText(printf("Total Expenses: €%.2f"))(totalExpenses)], react.createElement("span", {}, ...children_4)), (children_6 = [toText(printf("Balance: €%.2f"))(balance)], react.createElement("span", {}, ...children_6))], react.createElement("div", keyValueList(props_8, 1), ...children_8))), (props_22 = [["style", {
        display: "flex",
        marginBottom: "1rem",
    }]], (children_18 = [(props_10 = [new HTMLAttr(159, "text"), new HTMLAttr(128, "Description"), new HTMLAttr(161, model.DescriptionInput), new DOMAttr(9, (e) => {
        dispatch(new Msg(0, e.target.value));
    }), ["style", {
        padding: "0.5rem",
        marginRight: "0.5rem",
        flexGrow: "1",
        border: "1px solid #ccc",
        borderRadius: "4px",
    }]], react.createElement("input", keyValueList(props_10, 1))), (props_12 = [new HTMLAttr(159, "number"), new HTMLAttr(128, "Amount (in €)"), new HTMLAttr(161, model.AmountInput), new DOMAttr(9, (e_1) => {
        dispatch(new Msg(1, e_1.target.value));
    }), ["style", {
        padding: "0.5rem",
        marginRight: "0.5rem",
        width: "100px",
        border: "1px solid #ccc",
        borderRadius: "4px",
    }]], react.createElement("input", keyValueList(props_12, 1))), (props_18 = [new HTMLAttr(161, equals(model.TypeInput, new TransactionType(0)) ? "Expense" : "Income"), new DOMAttr(9, (e_2) => {
        dispatch(new Msg(2, ((e_2.target.value) === "Expense") ? (new TransactionType(0)) : (new TransactionType(1))));
    }), ["style", {
        padding: "0.5rem",
        marginRight: "0.5rem",
        border: "1px solid #ccc",
        borderRadius: "4px",
    }]], (children_14 = [react.createElement("option", {
        value: "Expense",
    }, "Expense"), react.createElement("option", {
        value: "Income",
    }, "Income")], react.createElement("select", keyValueList(props_18, 1), ...children_14))), (props_20 = [new DOMAttr(40, (_arg) => {
        dispatch(new Msg(3));
    }), ["style", {
        padding: "0.5rem 1rem",
        backgroundColor: "#007bff",
        color: "white",
        border: "none",
        borderRadius: "4px",
        cursor: "pointer",
    }]], react.createElement("button", keyValueList(props_20, 1), "Add"))], react.createElement("div", keyValueList(props_22, 1), ...children_18))), (props_30 = [["style", {
        listStyleType: "none",
        padding: "0",
    }]], (children_26 = map((tx) => {
        let children_20, arg_5, arg_3, props_26;
        const props_28 = [["style", {
            display: "flex",
            ["justify-content"]: "space-between",
            ["align-items"]: "center",
            padding: "0.5rem 0",
            borderBottom: "1px solid #eee",
        }]];
        const children_24 = [(children_20 = [(arg_5 = (equals(tx.Type, new TransactionType(0)) ? "-" : "+"), (arg_3 = toShortDateString(tx.Date), toText(printf("%s - %s: %s€%.2f"))(arg_3)(tx.Description)(arg_5)(tx.Amount)))], react.createElement("span", {}, ...children_20)), (props_26 = [new DOMAttr(40, (_arg_1) => {
            dispatch(new Msg(4, tx.Id));
        }), ["style", {
            padding: "0.25rem 0.5rem",
            backgroundColor: "#dc3545",
            color: "white",
            border: "none",
            borderRadius: "4px",
            cursor: "pointer",
        }]], react.createElement("button", keyValueList(props_26, 1), "Delete"))];
        return react.createElement("li", keyValueList(props_28, 1), ...children_24);
    }, model.Transactions), react.createElement("ul", keyValueList(props_30, 1), ...children_26)))];
    return react.createElement("div", keyValueList(props_32, 1), ...children_28);
}

ProgramModule_run(Program_withReactBatched("elmish-app", ProgramModule_mkProgram(init, update, view)));

