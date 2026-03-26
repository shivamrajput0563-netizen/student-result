
//  Student Result Management System
//  C# Console App | ADO.NET | SQL Server
//  File: Program.cs

using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace StudentResultMS
{
    //  CONNECTION HELPER
    public static class DbConfig
    {
        // Update Server name for your environment
        public static readonly string ConnectionString =
            "Server=.\\SQLEXPRESS;Database=StudentResultDB;Integrated Security=True;TrustServerCertificate=True;";
    }

    //  MODELS
    public class Student
    {
        public int    StudentID  { get; set; }
        public string RollNo     { get; set; } = "";
        public string FullName   { get; set; } = "";
        public string Email      { get; set; } = "";
        public string Course     { get; set; } = "";
        public int    Semester   { get; set; }
        public bool   IsActive   { get; set; }
    }

    public class Subject
    {
        public int    SubjectID   { get; set; }
        public string SubjectCode { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public int    MaxMarks    { get; set; }
        public int    Credits     { get; set; }
        public int    Semester    { get; set; }
    }

    public class Result
    {
        public string SubjectCode    { get; set; } = "";
        public string SubjectName    { get; set; } = "";
        public int    MaxMarks       { get; set; }
        public int    Credits        { get; set; }
        public int    MarksObtained  { get; set; }
        public string Grade          { get; set; } = "";
        public double GradePoint     { get; set; }
        public double Percentage     { get; set; }
    }

    public class SGPASummary
    {
        public string RollNo        { get; set; } = "";
        public string FullName      { get; set; } = "";
        public string Course        { get; set; } = "";
        public int    Semester      { get; set; }
        public int    TotalSubjects { get; set; }
        public int    TotalMarks    { get; set; }
        public int    MaxTotalMarks { get; set; }
        public double OverallPct    { get; set; }
        public double SGPA          { get; set; }
        public string PassStatus    { get; set; } = "";
    }

    //  DATA ACCESS LAYER
    public class StudentRepository
    {
        private readonly string _cs = DbConfig.ConnectionString;

        //GET ALL STUDENTS
        public DataTable GetAllStudents()
        {
            using var con = new SqlConnection(_cs);
            using var cmd = new SqlCommand(
                "SELECT StudentID, RollNo, FullName, Email, Course, Semester, EnrolledOn " +
                "FROM Students WHERE IsActive=1 ORDER BY RollNo", con);
            using var da  = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            con.Open();
            da.Fill(dt);
            return dt;
        }

        // GET STUDENT BY ID 
        public Student? GetStudentById(int id)
        {
            using var con = new SqlConnection(_cs);
            using var cmd = new SqlCommand(
                "SELECT * FROM Students WHERE StudentID=@id AND IsActive=1", con);
            cmd.Parameters.AddWithValue("@id", id);
            con.Open();
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            return new Student
            {
                StudentID = (int)    rdr["StudentID"],
                RollNo    = (string) rdr["RollNo"],
                FullName  = (string) rdr["FullName"],
                Email     = (string) rdr["Email"],
                Course    = (string) rdr["Course"],
                Semester  = (byte)   rdr["Semester"],
                IsActive  = (bool)   rdr["IsActive"]
            };
        }

        // ADD / UPDATE STUDENT 
        public int UpsertStudent(Student s)
        {
            using var con = new SqlConnection(_cs);
            using var cmd = new SqlCommand("sp_UpsertStudent", con)
            { CommandType = CommandType.StoredProcedure };

            if (s.StudentID > 0)
                cmd.Parameters.AddWithValue("@StudentID", s.StudentID);
            else
                cmd.Parameters.AddWithValue("@StudentID", DBNull.Value);

            cmd.Parameters.AddWithValue("@RollNo",   s.RollNo);
            cmd.Parameters.AddWithValue("@FullName", s.FullName);
            cmd.Parameters.AddWithValue("@Email",    s.Email);
            cmd.Parameters.AddWithValue("@Course",   s.Course);
            cmd.Parameters.AddWithValue("@Semester", s.Semester);

            con.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // SOFT-DELETE STUDENT 
        public void DeleteStudent(int id)
        {
            using var con = new SqlConnection(_cs);
            using var cmd = new SqlCommand("sp_DeleteStudent", con)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@StudentID", id);
            con.Open();
            cmd.ExecuteNonQuery();
        }
    }

    public class SubjectRepository
    {
        private readonly string _cs = DbConfig.ConnectionString;

        public DataTable GetAllSubjects()
        {
            using var con = new SqlConnection(_cs);
            using var cmd = new SqlCommand(
                "SELECT SubjectID, SubjectCode, SubjectName, MaxMarks, Credits, Semester " +
                "FROM Subjects ORDER BY Semester, SubjectCode", con);
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            con.Open();
            da.Fill(dt);
            return dt;
        }

        public Subject? GetSubjectById(int id)
        {
            using var con = new SqlConnection(_cs);
            using var cmd = new SqlCommand("SELECT * FROM Subjects WHERE SubjectID=@id", con);
            cmd.Parameters.AddWithValue("@id", id);
            con.Open();
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            return new Subject
            {
                SubjectID   = (int)    rdr["SubjectID"],
                SubjectCode = (string) rdr["SubjectCode"],
                SubjectName = (string) rdr["SubjectName"],
                MaxMarks    = (short)  rdr["MaxMarks"],
                Credits     = (byte)   rdr["Credits"],
                Semester    = (byte)   rdr["Semester"]
            };
        }

        public void AddSubject(Subject sub)
        {
            using var con = new SqlConnection(_cs);
            using var cmd = new SqlCommand(
                "INSERT INTO Subjects (SubjectCode,SubjectName,MaxMarks,Credits,Semester) " +
                "VALUES (@code,@name,@max,@cr,@sem)", con);
            cmd.Parameters.AddWithValue("@code", sub.SubjectCode);
            cmd.Parameters.AddWithValue("@name", sub.SubjectName);
            cmd.Parameters.AddWithValue("@max",  sub.MaxMarks);
            cmd.Parameters.AddWithValue("@cr",   sub.Credits);
            cmd.Parameters.AddWithValue("@sem",  sub.Semester);
            con.Open();
            cmd.ExecuteNonQuery();
        }
    }

    public class ResultRepository
    {
        private readonly string _cs = DbConfig.ConnectionString;

        // ENTER / UPDATE MARKS 
        public void EnterMarks(int studentId, int subjectId, int marks,
                               DateTime? examDate = null, string? remarks = null)
        {
            using var con = new SqlConnection(_cs);
            using var cmd = new SqlCommand("sp_EnterMarks", con)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@StudentID",     studentId);
            cmd.Parameters.AddWithValue("@SubjectID",     subjectId);
            cmd.Parameters.AddWithValue("@MarksObtained", marks);
            cmd.Parameters.AddWithValue("@ExamDate",      (object?)examDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Remarks",       (object?)remarks  ?? DBNull.Value);
            con.Open();
           cmd.ExecuteNonQuery();
        }

        //GET RESULT CARD
        public DataTable GetResultCard(int studentId)
        {
            using var con = new SqlConnection(_cs);
            using var cmd = new SqlCommand(
                "SELECT SubjectCode, SubjectName, MaxMarks, Credits, " +
                "MarksObtained, Grade, GradePoint, Percentage " +
                "FROM vw_StudentResults WHERE StudentID=@id", con);
            cmd.Parameters.AddWithValue("@id", studentId);
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            con.Open();
            da.Fill(dt);
            return dt;
        }

        // GET SGPA SUMMARY 
        public SGPASummary? GetSGPA(int studentId)
        {
            using var con = new SqlConnection(_cs);
            using var cmd = new SqlCommand(
                "SELECT * FROM vw_StudentSGPA WHERE StudentID=@id", con);
            cmd.Parameters.AddWithValue("@id", studentId);
            con.Open();
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            return new SGPASummary
            {
                RollNo        = (string) rdr["RollNo"],
                FullName      = (string) rdr["FullName"],
                Course        = (string) rdr["Course"],
                Semester      = (byte)   rdr["Semester"],
                TotalSubjects = (int)    rdr["TotalSubjects"],
                TotalMarks    = (int)    rdr["TotalMarks"],
                MaxTotalMarks = (int)    rdr["MaxTotalMarks"],
                OverallPct    = Convert.ToDouble(rdr["OverallPct"]),
                SGPA          = Convert.ToDouble(rdr["SGPA"]),
                PassStatus    = (string) rdr["PassStatus"]
            };
        }

        //  ALL SGPA RANKINGS 
        public DataTable GetAllSGPA()
        {
            using var con = new SqlConnection(_cs);
            using var cmd = new SqlCommand(
                "SELECT RollNo, FullName, Course, Semester, TotalSubjects, " +
                "OverallPct, SGPA, PassStatus FROM vw_StudentSGPA " +
                "ORDER BY SGPA DESC", con);
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            con.Open();
            da.Fill(dt);
            return dt;
        }
    }

    //  CONSOLE UI HELPER
  
    public static class UI
    {
        public static void Header(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine("╔" + new string('═', 60) + "╗");
            Console.WriteLine("║  " + title.PadRight(58) + "║");
            Console.WriteLine("╚" + new string('═', 60) + "╝");
            Console.ResetColor();
        }

        public static void Success(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✔  " + msg);
            Console.ResetColor();
        }

        public static void Error(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✖  " + msg);
            Console.ResetColor();
        }

        public static void Info(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  ℹ  " + msg);
            Console.ResetColor();
        }

        public static string Prompt(string label)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  → " + label + ": ");
            Console.ResetColor();
            return Console.ReadLine()?.Trim() ?? "";
        }

        public static int PromptInt(string label)
        {
            while (true)
            {
                string raw = Prompt(label);
                if (int.TryParse(raw, out int v)) return v;
                Error("Please enter a valid integer.");
            }
        }

        public static void PrintTable(DataTable dt, string[] cols, int[] widths)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            // Header row
            var sb = new StringBuilder("  │");
            for (int i = 0; i < cols.Length; i++)
                sb.Append(" " + cols[i].PadRight(widths[i]) + " │");
            Console.WriteLine(sb);
            Console.WriteLine("  ├" + string.Join("┼", Array.ConvertAll(widths, w => new string('─', w + 2))) + "┤");
            Console.ResetColor();

            foreach (DataRow row in dt.Rows)
            {
                Console.Write("  │");
                for (int i = 0; i < cols.Length; i++)
                {
                    string val = row[cols[i]]?.ToString() ?? "";
                    // Colour grade column
                    if (cols[i] == "Grade" || cols[i] == "PassStatus")
                    {
                        Console.ForegroundColor = val is "A+" or "A" or "PASS"
                            ? ConsoleColor.Green
                            : val is "F" or "FAIL"
                                ? ConsoleColor.Red
                                : ConsoleColor.Yellow;
                        Console.Write(" " + val.PadRight(widths[i]) + " ");
                        Console.ResetColor();
                    }
                    else
                        Console.Write(" " + val.PadRight(widths[i]) + " ");
                    Console.Write("│");
                }
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("  └" + string.Join("┴", Array.ConvertAll(widths, w => new string('─', w + 2))) + "┘");
            Console.ResetColor();
        }
    }

    //  APPLICATION CONTROLLER
    class Program
    {
        static readonly StudentRepository _students = new();
        static readonly SubjectRepository _subjects = new();
        static readonly ResultRepository  _results  = new();

        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Student Result Management System";

            while (true)
            {
                UI.Header("STUDENT RESULT MANAGEMENT SYSTEM");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("   [1]  Student Management (CRUD)");
                Console.WriteLine("   [2]  Subject Management");
                Console.WriteLine("   [3]  Enter / Update Marks");
                Console.WriteLine("   [4]  View Result Card");
                Console.WriteLine("   [5]  SGPA & Rankings");
                Console.WriteLine("   [0]  Exit");
                Console.ResetColor();
                Console.WriteLine();

                string choice = UI.Prompt("Select option");
                switch (choice)
                {
                    case "1": StudentMenu();  break;
                    case "2": SubjectMenu();  break;
                    case "3": EnterMarks();   break;
                    case "4": ViewResult();   break;
                    case "5": ViewRankings(); break;
                    case "0":
                        UI.Success("Goodbye!");
                        return;
                    default:
                        UI.Error("Invalid option. Try again.");
                        break;
                }
            }
        }

        //  STUDENT MENU
        static void StudentMenu()
        {
            UI.Header("STUDENT MANAGEMENT");
            Console.WriteLine("   [1] List All Students");
            Console.WriteLine("   [2] Add New Student");
            Console.WriteLine("   [3] Update Student");
            Console.WriteLine("   [4] Delete Student");
            Console.WriteLine("   [0] Back");

            string ch = UI.Prompt("Select");
            switch (ch)
            {
                case "1": ListStudents(); break;
                case "2": AddStudent();   break;
                case "3": UpdateStudent(); break;
                case "4": DeleteStudent(); break;
            }
        }

        static void ListStudents()
        {
            UI.Header("ALL STUDENTS");
            var dt = _students.GetAllStudents();
            if (dt.Rows.Count == 0) { UI.Info("No students found."); return; }

            string[] cols   = { "StudentID", "RollNo", "FullName", "Email", "Course", "Semester" };
            int[]    widths = { 9, 8, 20, 28, 14, 8 };
            UI.PrintTable(dt, cols, widths);
            UI.Info($"Total: {dt.Rows.Count} student(s)");
        }

        static Student ReadStudentInput(int id = 0)
        {
            var s = new Student { StudentID = id };
            s.RollNo   = UI.Prompt("Roll No");
            s.FullName = UI.Prompt("Full Name");
            s.Email    = UI.Prompt("Email");
            s.Course   = UI.Prompt("Course");
            s.Semester = UI.PromptInt("Semester (1-8)");
            return s;
        }

        static void AddStudent()
        {
            UI.Header("ADD STUDENT");
            var s = ReadStudentInput();
            try
            {
                int newId = _students.UpsertStudent(s);
                UI.Success($"Student added with ID = {newId}");
            }
            catch (SqlException ex)
            {
                UI.Error($"DB Error: {ex.Message}");
            }
        }

        static void UpdateStudent()
        {
            UI.Header("UPDATE STUDENT");
            ListStudents();
            int id = UI.PromptInt("Enter StudentID to update");
            var existing = _students.GetStudentById(id);
            if (existing == null) { UI.Error("Student not found."); return; }

            UI.Info($"Editing: {existing.FullName} ({existing.RollNo})");
            var updated = ReadStudentInput(id);
            _students.UpsertStudent(updated);
            UI.Success("Student updated successfully.");
        }

        static void DeleteStudent()
        {
            UI.Header("DELETE STUDENT");
            ListStudents();
            int id = UI.PromptInt("Enter StudentID to delete");
            var existing = _students.GetStudentById(id);
            if (existing == null) { UI.Error("Student not found."); return; }

            string confirm = UI.Prompt($"Delete '{existing.FullName}'? (yes/no)");
            if (confirm.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                _students.DeleteStudent(id);
                UI.Success("Student soft-deleted.");
            }
            else UI.Info("Cancelled.");
        }

        //  SUBJECT MENU
        static void SubjectMenu()
        {
            UI.Header("SUBJECT MANAGEMENT");
            Console.WriteLine("   [1] List All Subjects");
            Console.WriteLine("   [2] Add New Subject");
            Console.WriteLine("   [0] Back");

            string ch = UI.Prompt("Select");
            switch (ch)
            {
                case "1": ListSubjects(); break;
                case "2": AddSubject();   break;
            }
        }

        static void ListSubjects()
        {
            UI.Header("ALL SUBJECTS");
            var dt = _subjects.GetAllSubjects();
            if (dt.Rows.Count == 0) { UI.Info("No subjects found."); return; }

            string[] cols   = { "SubjectID", "SubjectCode", "SubjectName", "MaxMarks", "Credits", "Semester" };
            int[]    widths = { 9, 11, 35, 9, 7, 8 };
            UI.PrintTable(dt, cols, widths);
        }

        static void AddSubject()
        {
            UI.Header("ADD SUBJECT");
            var sub = new Subject
            {
                SubjectCode = UI.Prompt("Subject Code"),
                SubjectName = UI.Prompt("Subject Name"),
                MaxMarks    = UI.PromptInt("Max Marks"),
                Credits     = UI.PromptInt("Credits"),
                Semester    = UI.PromptInt("Semester")
            };
            try
            {
                _subjects.AddSubject(sub);
                UI.Success("Subject added.");
            }
            catch (SqlException ex) { UI.Error(ex.Message); }
        }

        //  ENTER MARKS
        static void EnterMarks()
        {
            UI.Header("ENTER / UPDATE MARKS");
            ListStudents();
            int sid = UI.PromptInt("Student ID");

            ListSubjects();
            int subId = UI.PromptInt("Subject ID");

            var sub = _subjects.GetSubjectById(subId);
            if (sub == null) { UI.Error("Subject not found."); return; }

            int marks = UI.PromptInt($"Marks Obtained (Max: {sub.MaxMarks})");
            if (marks < 0 || marks > sub.MaxMarks)
            { UI.Error("Marks out of range."); return; }

            string remarks = UI.Prompt("Remarks (optional)");

            _results.EnterMarks(sid, subId, marks, DateTime.Today,
                                string.IsNullOrWhiteSpace(remarks) ? null : remarks);
            UI.Success("Marks saved.");
        }

        //  VIEW RESULT CARD
        static void ViewResult()
        {
            UI.Header("VIEW RESULT CARD");
            ListStudents();
            int sid = UI.PromptInt("Student ID");

            var student = _students.GetStudentById(sid);
            if (student == null) { UI.Error("Student not found."); return; }

            var dt = _results.GetResultCard(sid);
            if (dt.Rows.Count == 0) { UI.Info("No results found for this student."); return; }

            UI.Header($"RESULT CARD — {student.FullName} ({student.RollNo})");
            UI.Info($"Course: {student.Course}  |  Semester: {student.Semester}");
            Console.WriteLine();

            string[] cols   = { "SubjectCode", "SubjectName", "MaxMarks", "Credits",
                                 "MarksObtained", "Percentage", "Grade", "GradePoint" };
            int[]    widths = { 11, 32, 9, 7, 14, 11, 5, 10 };
            UI.PrintTable(dt, cols, widths);

            var sgpa = _results.GetSGPA(sid);
            if (sgpa != null)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  ┌─ SUMMARY ───────────────────────────────┐");
                Console.WriteLine($"  │  Total Marks  : {sgpa.TotalMarks}/{sgpa.MaxTotalMarks}".PadRight(46) + "│");
                Console.WriteLine($"  │  Overall %    : {sgpa.OverallPct:F2}%".PadRight(46) + "│");
                Console.WriteLine($"  │  SGPA         : {sgpa.SGPA:F2}".PadRight(46) + "│");
                Console.ForegroundColor = sgpa.PassStatus == "PASS"
                    ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine($"  │  Status       : {sgpa.PassStatus}".PadRight(46) + "│");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  └─────────────────────────────────────────┘");
                Console.ResetColor();
            }
        }

        //  SGPA RANKINGS
        static void ViewRankings()
        {
            UI.Header("SGPA RANKINGS — ALL STUDENTS");
            var dt = _results.GetAllSGPA();
            if (dt.Rows.Count == 0) { UI.Info("No results available."); return; }

            int rank = 1;
            foreach (DataRow row in dt.Rows)
            {
                Console.ForegroundColor = rank == 1 ? ConsoleColor.Yellow
                                        : rank == 2 ? ConsoleColor.Gray
                                        : rank == 3 ? ConsoleColor.DarkYellow
                                        : ConsoleColor.White;
                Console.WriteLine($"  #{rank,2}  {row["FullName"],-22}  Roll: {row["RollNo"],-8}" +
                                  $"  SGPA: {row["SGPA"],5}  {row["OverallPct"],6}%  [{row["PassStatus"]}]");
                Console.ResetColor();
                rank++;
            }
        }
    }
}
