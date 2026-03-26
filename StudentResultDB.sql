--  Student Result Management System — Database Schema
--  SQL Server | 3 Normalized Relational Tables
USE master;
GO

--Create Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'StudentResultDB')
BEGIN
    CREATE DATABASE StudentResultDB;
END
GO

USE StudentResultDB;
GO

-- TABLE 1: Students

IF OBJECT_ID('dbo.Students', 'U') IS NOT NULL DROP TABLE dbo.Students;
GO

CREATE TABLE dbo.Students (
    StudentID   INT           IDENTITY(1,1) PRIMARY KEY,
    RollNo      VARCHAR(20)   NOT NULL UNIQUE,
    FullName    NVARCHAR(100) NOT NULL,
    Email       VARCHAR(150)  NOT NULL UNIQUE,
    Course      NVARCHAR(100) NOT NULL,
    Semester    TINYINT       NOT NULL CHECK (Semester BETWEEN 1 AND 8),
    EnrolledOn  DATE          NOT NULL DEFAULT GETDATE(),
    IsActive    BIT           NOT NULL DEFAULT 1
);
GO

-- TABLE 2: Subjects
IF OBJECT_ID('dbo.Subjects', 'U') IS NOT NULL DROP TABLE dbo.Subjects;
GO

CREATE TABLE dbo.Subjects (
    SubjectID   INT           IDENTITY(1,1) PRIMARY KEY,
    SubjectCode VARCHAR(20)   NOT NULL UNIQUE,
    SubjectName NVARCHAR(150) NOT NULL,
    MaxMarks    SMALLINT      NOT NULL DEFAULT 100 CHECK (MaxMarks > 0),
    Credits     TINYINT       NOT NULL DEFAULT 3 CHECK (Credits BETWEEN 1 AND 6),
    Semester    TINYINT       NOT NULL CHECK (Semester BETWEEN 1 AND 8)
);
GO

-- TABLE 3: Results (Marks + Auto Grade)

IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
GO

CREATE TABLE dbo.Results (
    ResultID      INT        IDENTITY(1,1) PRIMARY KEY,
    StudentID     INT        NOT NULL REFERENCES dbo.Students(StudentID) ON DELETE CASCADE,
    SubjectID     INT        NOT NULL REFERENCES dbo.Subjects(SubjectID) ON DELETE CASCADE,
    MarksObtained SMALLINT   NOT NULL CHECK (MarksObtained >= 0),
    Grade         CHAR(2)    NOT NULL,           -- Computed & stored on insert/update
    GradePoint    DECIMAL(3,1) NOT NULL,
    ExamDate      DATE        NOT NULL DEFAULT GETDATE(),
    Remarks       NVARCHAR(200) NULL,
    CONSTRAINT UQ_Student_Subject UNIQUE (StudentID, SubjectID)
);
GO

-- Grade Calculator 

CREATE OR ALTER FUNCTION dbo.fn_CalculateGrade (
    @MarksObtained SMALLINT,
    @MaxMarks      SMALLINT
)
RETURNS TABLE
AS
RETURN (
    SELECT
        Percentage = CAST(@MarksObtained * 100.0 / @MaxMarks AS DECIMAL(5,2)),
        Grade = CASE
            WHEN @MarksObtained * 100.0 / @MaxMarks >= 90 THEN 'A+'
            WHEN @MarksObtained * 100.0 / @MaxMarks >= 80 THEN 'A'
            WHEN @MarksObtained * 100.0 / @MaxMarks >= 70 THEN 'B+'
            WHEN @MarksObtained * 100.0 / @MaxMarks >= 60 THEN 'B'
            WHEN @MarksObtained * 100.0 / @MaxMarks >= 50 THEN 'C'
            WHEN @MarksObtained * 100.0 / @MaxMarks >= 40 THEN 'D'
            ELSE 'F'
        END,
        GradePoint = CASE
            WHEN @MarksObtained * 100.0 / @MaxMarks >= 90 THEN 10.0
            WHEN @MarksObtained * 100.0 / @MaxMarks >= 80 THEN 9.0
            WHEN @MarksObtained * 100.0 / @MaxMarks >= 70 THEN 8.0
            WHEN @MarksObtained * 100.0 / @MaxMarks >= 60 THEN 7.0
            WHEN @MarksObtained * 100.0 / @MaxMarks >= 50 THEN 6.0
            WHEN @MarksObtained * 100.0 / @MaxMarks >= 40 THEN 5.0
            ELSE 0.0
        END
);
GO

-- Add / Update Student

CREATE OR ALTER PROCEDURE dbo.sp_UpsertStudent
    @StudentID  INT           = NULL,  -- NULL = Insert, non-NULL = Update
    @RollNo     VARCHAR(20),
    @FullName   NVARCHAR(100),
    @Email      VARCHAR(150),
    @Course     NVARCHAR(100),
    @Semester   TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    IF @StudentID IS NULL
    BEGIN
        INSERT INTO dbo.Students (RollNo, FullName, Email, Course, Semester)
        VALUES (@RollNo, @FullName, @Email, @Course, @Semester);
        SELECT SCOPE_IDENTITY() AS NewStudentID;
    END
    ELSE
    BEGIN
        UPDATE dbo.Students
        SET RollNo   = @RollNo,
            FullName = @FullName,
            Email    = @Email,
            Course   = @Course,
            Semester = @Semester
        WHERE StudentID = @StudentID;
        SELECT @StudentID AS NewStudentID;
    END
END
GO

-- Delete Student (Soft Delete)
CREATE OR ALTER PROCEDURE dbo.sp_DeleteStudent
    @StudentID INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Students SET IsActive = 0 WHERE StudentID = @StudentID;
END
GO


-- Enter / Update Marks
CREATE OR ALTER PROCEDURE dbo.sp_EnterMarks
    @StudentID     INT,
    @SubjectID     INT,
    @MarksObtained SMALLINT,
    @ExamDate      DATE = NULL,
    @Remarks       NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MaxMarks SMALLINT;
    SELECT @MaxMarks = MaxMarks FROM dbo.Subjects WHERE SubjectID = @SubjectID;

    DECLARE @Grade      CHAR(2);
    DECLARE @GradePoint DECIMAL(3,1);

    SELECT @Grade = Grade, @GradePoint = GradePoint
    FROM dbo.fn_CalculateGrade(@MarksObtained, @MaxMarks);

    IF EXISTS (SELECT 1 FROM dbo.Results WHERE StudentID = @StudentID AND SubjectID = @SubjectID)
    BEGIN
        UPDATE dbo.Results
        SET MarksObtained = @MarksObtained,
            Grade         = @Grade,
            GradePoint    = @GradePoint,
            ExamDate      = ISNULL(@ExamDate, GETDATE()),
            Remarks       = @Remarks
        WHERE StudentID = @StudentID AND SubjectID = @SubjectID;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Results (StudentID, SubjectID, MarksObtained, Grade, GradePoint, ExamDate, Remarks)
        VALUES (@StudentID, @SubjectID, @MarksObtained, @Grade, @GradePoint, ISNULL(@ExamDate, GETDATE()), @Remarks);
    END
END
GO


-- VIEW: Full Result Card per Student

CREATE OR ALTER VIEW dbo.vw_StudentResults AS
    SELECT
        s.StudentID,
        s.RollNo,
        s.FullName,
        s.Course,
        s.Semester,
        sub.SubjectCode,
        sub.SubjectName,
        sub.MaxMarks,
        sub.Credits,
        r.MarksObtained,
        r.Grade,
        r.GradePoint,
        r.ExamDate,
        Percentage = CAST(r.MarksObtained * 100.0 / sub.MaxMarks AS DECIMAL(5,2))
    FROM dbo.Students    s
    JOIN dbo.Results     r   ON r.StudentID  = s.StudentID
    JOIN dbo.Subjects    sub ON sub.SubjectID = r.SubjectID
    WHERE s.IsActive = 1;
GO

-- VIEW: SGPA Summary per Student

CREATE OR ALTER VIEW dbo.vw_StudentSGPA AS
    SELECT
        s.StudentID,
        s.RollNo,
        s.FullName,
        s.Course,
        s.Semester,
        TotalSubjects = COUNT(r.ResultID),
        TotalMarks    = SUM(r.MarksObtained),
        MaxTotalMarks = SUM(sub.MaxMarks),
        OverallPct    = CAST(SUM(r.MarksObtained) * 100.0 / NULLIF(SUM(sub.MaxMarks), 0) AS DECIMAL(5,2)),
        SGPA          = CAST(
                            SUM(r.GradePoint * sub.Credits) * 1.0 /
                            NULLIF(SUM(sub.Credits), 0)
                        AS DECIMAL(4,2)),
        PassStatus    = CASE WHEN MIN(r.MarksObtained * 100 / sub.MaxMarks) >= 40 THEN 'PASS' ELSE 'FAIL' END
    FROM dbo.Students s
    JOIN dbo.Results  r   ON r.StudentID  = s.StudentID
    JOIN dbo.Subjects sub ON sub.SubjectID = r.SubjectID
    WHERE s.IsActive = 1
    GROUP BY s.StudentID, s.RollNo, s.FullName, s.Course, s.Semester;
GO

-- SEED DATA — Sample Students

INSERT INTO dbo.Students (RollNo, FullName, Email, Course, Semester, EnrolledOn) VALUES
('CS2401', 'Arjun Sharma',    'arjun.sharma@college.edu',   'B.Tech CSE', 4, '2024-07-01'),
('CS2402', 'Priya Mehra',     'priya.mehra@college.edu',    'B.Tech CSE', 4, '2024-07-01'),
('CS2403', 'Rohan Verma',     'rohan.verma@college.edu',    'B.Tech CSE', 4, '2024-07-01'),
('CS2404', 'Sneha Gupta',     'sneha.gupta@college.edu',    'B.Tech CSE', 4, '2024-07-01'),
('CS2405', 'Karan Singh',     'karan.singh@college.edu',    'B.Tech CSE', 4, '2024-07-01');

-- Sample Subjects (Sem 4, B.Tech CSE)
INSERT INTO dbo.Subjects (SubjectCode, SubjectName, MaxMarks, Credits, Semester) VALUES
('CS401', 'Data Structures & Algorithms', 100, 4, 4),
('CS402', 'Database Management Systems',  100, 4, 4),
('CS403', 'Operating Systems',            100, 3, 4),
('CS404', 'Computer Networks',            100, 3, 4),
('CS405', 'Software Engineering',         100, 3, 4);

-- Sample Marks
EXEC dbo.sp_EnterMarks @StudentID=1, @SubjectID=1, @MarksObtained=88, @ExamDate='2024-12-01';
EXEC dbo.sp_EnterMarks @StudentID=1, @SubjectID=2, @MarksObtained=76, @ExamDate='2024-12-02';
EXEC dbo.sp_EnterMarks @StudentID=1, @SubjectID=3, @MarksObtained=92, @ExamDate='2024-12-03';
EXEC dbo.sp_EnterMarks @StudentID=1, @SubjectID=4, @MarksObtained=65, @ExamDate='2024-12-04';
EXEC dbo.sp_EnterMarks @StudentID=1, @SubjectID=5, @MarksObtained=81, @ExamDate='2024-12-05';

EXEC dbo.sp_EnterMarks @StudentID=2, @SubjectID=1, @MarksObtained=95, @ExamDate='2024-12-01';
EXEC dbo.sp_EnterMarks @StudentID=2, @SubjectID=2, @MarksObtained=89, @ExamDate='2024-12-02';
EXEC dbo.sp_EnterMarks @StudentID=2, @SubjectID=3, @MarksObtained=78, @ExamDate='2024-12-03';
EXEC dbo.sp_EnterMarks @StudentID=2, @SubjectID=4, @MarksObtained=91, @ExamDate='2024-12-04';
EXEC dbo.sp_EnterMarks @StudentID=2, @SubjectID=5, @MarksObtained=87, @ExamDate='2024-12-05';

EXEC dbo.sp_EnterMarks @StudentID=3, @SubjectID=1, @MarksObtained=55, @ExamDate='2024-12-01';
EXEC dbo.sp_EnterMarks @StudentID=3, @SubjectID=2, @MarksObtained=48, @ExamDate='2024-12-02';
EXEC dbo.sp_EnterMarks @StudentID=3, @SubjectID=3, @MarksObtained=62, @ExamDate='2024-12-03';
EXEC dbo.sp_EnterMarks @StudentID=3, @SubjectID=4, @MarksObtained=37, @ExamDate='2024-12-04';
EXEC dbo.sp_EnterMarks @StudentID=3, @SubjectID=5, @MarksObtained=71, @ExamDate='2024-12-05';

EXEC dbo.sp_EnterMarks @StudentID=4, @SubjectID=1, @MarksObtained=72, @ExamDate='2024-12-01';
EXEC dbo.sp_EnterMarks @StudentID=4, @SubjectID=2, @MarksObtained=68, @ExamDate='2024-12-02';
EXEC dbo.sp_EnterMarks @StudentID=4, @SubjectID=3, @MarksObtained=84, @ExamDate='2024-12-03';
EXEC dbo.sp_EnterMarks @StudentID=4, @SubjectID=4, @MarksObtained=77, @ExamDate='2024-12-04';
EXEC dbo.sp_EnterMarks @StudentID=4, @SubjectID=5, @MarksObtained=90, @ExamDate='2024-12-05';

EXEC dbo.sp_EnterMarks @StudentID=5, @SubjectID=1, @MarksObtained=43, @ExamDate='2024-12-01';
EXEC dbo.sp_EnterMarks @StudentID=5, @SubjectID=2, @MarksObtained=58, @ExamDate='2024-12-02';
EXEC dbo.sp_EnterMarks @StudentID=5, @SubjectID=3, @MarksObtained=66, @ExamDate='2024-12-03';
EXEC dbo.sp_EnterMarks @StudentID=5, @SubjectID=4, @MarksObtained=49, @ExamDate='2024-12-04';
EXEC dbo.sp_EnterMarks @StudentID=5, @SubjectID=5, @MarksObtained=74, @ExamDate='2024-12-05';
GO

PRINT 'StudentResultDB created and seeded successfully.';
GO
