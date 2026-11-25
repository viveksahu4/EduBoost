using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduBoost.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
GROUP BY s.Id, s.RollNo, s.FullName, s.ClassName, s.AttendancePercent, s.RiskScore, s.IsSlowLearner;");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_GetSlowLearners
    @MinRisk FLOAT = 45
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, RollNo, FullName, ClassName, RiskScore, AttendancePercent
    FROM Students
    WHERE RiskScore >= @MinRisk OR IsSlowLearner = 1
    ORDER BY RiskScore DESC;
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetSlowLearners;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_StudentPerformance;");
        }
    }
}
