# Student Portal v6

An offline-first student portal prototype with one working ASP.NET Core Account API and a plain HTML/CSS/JavaScript frontend.

## File map

```text
student-portal-v6/
├── api/
│   ├── accountapi/                 # Working API: create/login accounts
│   │   ├── Controllers/
│   │   │   └── AccountController.cs
│   │   ├── Models/
│   │   │   └── AccountModels.cs
│   │   ├── AccountAPI.csproj
│   │   └── Program.cs
│   ├── Feature02API/               # Empty placeholder for future API
│   ├── Feature03API/               # Empty placeholder for future API
│   └── Feature04API/               # Empty placeholder for future API
│
├── frontend/
│   ├── Login/
│   │   ├── index.html              # Login / create account page
│   │   └── login.js
│   ├── assets/
│   │   └── style.css               # Shared styles + marked feature blocks
│   ├── index.html                  # Single main portal page
│   └── app.js                      # Portal behavior + marked feature blocks
│
└── README.md
```

## Main page organization

Feature 01 (`Student Portal`) is one page containing three sections in order:

1. Student Information
2. General Transactions
3. Grades

Transactions and Grades are **not** separate sidebar features. Sidebar entries 02, 03 and 04 are reserved for future additions.

The Grades section contains fictional example subjects and a semester selector. The Transactions section currently uses browser localStorage until a transaction API is added.

## Account API

Student numbers are unique. The Account API rejects attempts to create a second account with the same normalized student number.

Start it from the project root:

```powershell
cd api/accountapi
dotnet run
```

Then open `frontend/Login/index.html` in the browser. The frontend itself is plain HTML/CSS/JS and does not require `dotnet run`.

> Browser security settings can restrict `file://` pages from calling localhost HTTP APIs. If that occurs, use a local static-file server during development; the frontend architecture itself remains static HTML/CSS/JS.

## Adding future features

Each placeholder has clearly marked sections in `frontend/index.html`, `frontend/app.js`, and `frontend/assets/style.css`. When Feature 02 is decided, rename or populate `api/Feature02API/` and replace only the corresponding placeholder blocks. Repeat the same pattern for Features 03 and 04.
