# Student Portal v7

A plain HTML/CSS/JavaScript student portal backed by small ASP.NET Core APIs.

## Features

### Login / Accounts
- Student login.
- Administrator login using the requested demo credentials:
  - Username: `Dragonborn`
  - Password: `Fusrodah`
- New student accounts automatically receive a school number beginning with the current year. The generated format is `SYYYYCCSSSSS`, where `YYYY` is the current year, `CC` is the current student-count sequence, and `SSSSS` is the five-digit sequence.
- Program and Year Level are dropdowns on account creation.

### Feature 01 — Student Portal
- Student information.
- Transactions are blank until an administrator records them.
- Grades are blank until grades are recorded.
- Admin can list and edit students, including program, year level, and class/section.
- Admin can add transactions for a student, program, and class/section.

### Feature 02 — Announcements
- Everyone announcements.
- Program-specific announcements.
- Student-specific Personal Announcements.
- Admin publishing and deletion.
- Lost & Found notifications are published as student-specific announcements.

### Feature 03 — Lost & Found
- Students can submit Lost or Found reports.
- Admin can view reports.
- Admin can notify the student through the Announcements API, creating a Personal Announcement.

### Feature 04 — Library Access
- Intentionally blank for now.

## Ports

Start each API in its own terminal:

```powershell
cd api/accountapi
dotnet run
```
Runs on `http://localhost:5002`.

```powershell
cd api/announcementapi
dotnet run
```
Runs on `http://localhost:5003`.

```powershell
cd api/Feature03API
dotnet run
```
Runs on `http://localhost:5004`.

Then open `frontend/Login/index.html` through a local static server.

## Notes

This is still a school-project prototype. JSON files are used for storage and the requested admin credentials are hard-coded for demonstration. For a production system, move authentication to a database and use hashed/managed administrator credentials with proper authorization tokens.
