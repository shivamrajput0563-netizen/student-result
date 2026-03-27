# 🎓 Student Result Management System

[cite_start]A robust, full-stack academic performance tracking system built with **C#**, **SQL Server**, and a modern **Web Dashboard**. This project demonstrates the use of **ADO.NET** for database operations, stored procedures for business logic, and a responsive frontend for data visualization.

---

## 🚀 Features

### **Backend (C# Console Application)**
* **Student CRUD:** Full management of student profiles including Add, Update, List, and Soft-delete operations.
* **Subject Management:** Define subject codes, credit weightage, and semester-wise mapping.
* **Automated Grade Calculation:** Marks are automatically converted to Grades (A+ to F) and Grade Points (0.0 to 10.0) using SQL functions.
* **SGPA & Rankings:** Real-time calculation of Semester Grade Point Average (SGPA) and class leaderboards.

### **Frontend (Web Dashboard)**
* **Modern UI:** A sleek, "dark-mode" influenced interface using CSS Grid and Flexbox.
* **Data Visualization:** Interactive stats cards for pass rates, average SGPA, and student enrollments.
* **Result Cards:** Detailed, printable-style student mark sheets with percentage progress bars.
* **Leaderboard:** Visual ranking system with Gold, Silver, and Bronze badges for top performers.
---

## 🛠️ Tech Stack

* **Language:** C# (.NET 8.0).
* **Database:** Microsoft SQL Server.
* **Data Access:** ADO.NET (Microsoft.Data.SqlClient).
* [cite_start]**Frontend:** HTML5, CSS3, and Vanilla JavaScript.

---

## 📋 Database Architecture

The system uses a normalized relational schema with three primary tables:
1.  **`Students`**: Stores roll numbers, names, emails, and enrollment status.
2.  **`Subjects`**: Manages academic courses, max marks, and credit values.
3.  **`Results`**: Tracks obtained marks and links students to their subjects.

**Key Logic:** Grade calculation is handled at the database level via the `fn_CalculateGrade` function to ensure consistency between the C# application and the Web UI.

---

## ⚙️ Setup & Installation

### 1. Database Setup
1.  Open **SQL Server Management Studio (SSMS)**.
2.  Execute the provided `StudentResultDB.sql` script to create the database, tables, and stored procedures.
3.  The script includes seed data for 5 sample students and subjects to get you started.

### 2. Configure Connection String
1.  Open `Program.cs`.
2.  Update the `DbConfig` class with your local server name:
    ```csharp
    public static readonly string ConnectionString = "Server=.\\SQLEXPRESS;Database=StudentResultDB;Integrated Security=True;TrustServerCertificate=True;";
    ```

### 3. Run the Application
1.  Ensure you have the .NET 8 SDK installed.
2.  Run the project using your IDE or terminal:
    ```bash
    dotnet run
    ```

---

## 📂 Project Structure

* **`Program.cs`**: Main application logic and ADO.NET repositories.
* **`StudentResultDB.sql`**: Complete database schema and stored procedures.
* [cite_start]**`index.html`**: Responsive administrative dashboard.
* **`StudentResultMS.csproj`**: Project configuration and dependencies.
## 📷 Screenshots
<img width="1919" height="983" alt="image" src="https://github.com/user-attachments/assets/11866585-5115-4433-a75a-e50c7ba57deb" />
<img width="1919" height="966" alt="image" src="https://github.com/user-attachments/assets/fa6bd194-53ee-4519-bd18-db4695dd95d3" />

