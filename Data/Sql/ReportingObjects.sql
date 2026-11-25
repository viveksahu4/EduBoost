-- EduBoost reporting objects (SQL Server / LocalDB)
-- These are also applied automatically by the EF Core migration
-- 20261118142609_AddReportingObjects. This file is kept for the project report
-- and for running the objects manually in SSMS / SQL Server Object Explorer.

CREATE OR ALTER VIEW vw_StudentPerformance AS
SELECT
    s.Id,
    s.RollNo,
    s.FullName,
    s.ClassName,
    s.AttendancePercent,
    s.RiskScore,
    s.IsSlowLearner,
    CAST(ISNULL(AVG(a.MarksObtained * 100.0 / NULLIF(a.MaxMarks, 0)), 0) AS FLOAT) AS AvgPercent
FROM Students s
LEFT JOIN Assessments a ON a.StudentId = s.Id
GROUP BY s.Id, s.RollNo, s.FullName, s.ClassName, s.AttendancePercent, s.RiskScore, s.IsSlowLearner;
GO

CREATE OR ALTER PROCEDURE sp_GetSlowLearners
    @MinRisk FLOAT = 45
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, RollNo, FullName, ClassName, RiskScore, AttendancePercent
    FROM Students
    WHERE RiskScore >= @MinRisk OR IsSlowLearner = 1
    ORDER BY RiskScore DESC;
END;
GO

-- Test queries:
-- SELECT * FROM vw_StudentPerformance ORDER BY AvgPercent;
-- EXEC sp_GetSlowLearners @MinRisk = 50;
