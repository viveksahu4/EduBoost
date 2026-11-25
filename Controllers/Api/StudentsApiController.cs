using EduBoost.Data;
using EduBoost.Models;
using EduBoost.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBoost.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/students")]
    [Produces("application/json")]
    public class StudentsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public StudentsApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Retrieves all enrolled students mapped to DTOs.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<StudentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var students = await _db.Students
                .AsNoTracking()
                .Include(s => s.Assessments)
                .Include(s => s.RemedialPlans)
                .OrderBy(s => s.RollNo)
                .Select(s => new StudentDto
                {
                    Id = s.Id,
                    RollNo = s.RollNo,
                    FullName = s.FullName,
                    ClassName = s.ClassName,
                    ParentEmail = s.ParentEmail,
                    AttendancePercent = s.AttendancePercent,
                    LearningStyle = s.LearningStyle,
                    RiskScore = s.RiskScore,
                    IsSlowLearner = s.IsSlowLearner,
                    AssessmentCount = s.Assessments.Count,
                    ActivePlansCount = s.RemedialPlans.Count(p => p.Status == "Active")
                })
                .ToListAsync();

            return Ok(students);
        }

        /// <summary>
        /// Retrieves only students flagged as slow learners.
        /// </summary>
        [HttpGet("slow")]
        [ProducesResponseType(typeof(IEnumerable<StudentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSlow()
        {
            var slowStudents = await _db.Students
                .AsNoTracking()
                .Include(s => s.Assessments)
                .Include(s => s.RemedialPlans)
                .Where(s => s.IsSlowLearner)
                .OrderByDescending(s => s.RiskScore)
                .Select(s => new StudentDto
                {
                    Id = s.Id,
                    RollNo = s.RollNo,
                    FullName = s.FullName,
                    ClassName = s.ClassName,
                    ParentEmail = s.ParentEmail,
                    AttendancePercent = s.AttendancePercent,
                    LearningStyle = s.LearningStyle,
                    RiskScore = s.RiskScore,
                    IsSlowLearner = s.IsSlowLearner,
                    AssessmentCount = s.Assessments.Count,
                    ActivePlansCount = s.RemedialPlans.Count(p => p.Status == "Active")
                })
                .ToListAsync();

            return Ok(slowStudents);
        }

        /// <summary>
        /// Retrieves a specific student by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id)
        {
            var s = await _db.Students
                .AsNoTracking()
                .Include(s => s.Assessments)
                .Include(s => s.RemedialPlans)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (s == null) return NotFound();

            return Ok(new StudentDto
            {
                Id = s.Id,
                RollNo = s.RollNo,
                FullName = s.FullName,
                ClassName = s.ClassName,
                ParentEmail = s.ParentEmail,
                AttendancePercent = s.AttendancePercent,
                LearningStyle = s.LearningStyle,
                RiskScore = s.RiskScore,
                IsSlowLearner = s.IsSlowLearner,
                AssessmentCount = s.Assessments.Count,
                ActivePlansCount = s.RemedialPlans.Count(p => p.Status == "Active")
            });
        }

        /// <summary>
        /// Enrolls a new student.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(StudentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] CreateStudentDto dto)
        {
            if (await _db.Students.AnyAsync(s => s.RollNo == dto.RollNo))
            {
                ModelState.AddModelError(nameof(dto.RollNo), "This roll number already exists.");
                return ValidationProblem(ModelState);
            }

            var student = new Student
            {
                RollNo = dto.RollNo.Trim(),
                FullName = dto.FullName.Trim(),
                ClassName = dto.ClassName.Trim(),
                ParentEmail = dto.ParentEmail?.Trim(),
                AttendancePercent = dto.AttendancePercent,
                LearningStyle = string.IsNullOrWhiteSpace(dto.LearningStyle) ? "Visual" : dto.LearningStyle
            };

            // Calculate initial attendance risk
            student.RiskScore = Math.Round((100 - student.AttendancePercent) * 0.25, 1);
            student.IsSlowLearner = student.RiskScore >= 45;

            _db.Students.Add(student);
            await _db.SaveChangesAsync();

            var resultDto = new StudentDto
            {
                Id = student.Id,
                RollNo = student.RollNo,
                FullName = student.FullName,
                ClassName = student.ClassName,
                ParentEmail = student.ParentEmail,
                AttendancePercent = student.AttendancePercent,
                LearningStyle = student.LearningStyle,
                RiskScore = student.RiskScore,
                IsSlowLearner = student.IsSlowLearner
            };

            return CreatedAtAction(nameof(Get), new { id = student.Id }, resultDto);
        }

        /// <summary>
        /// Updates an existing student record.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateStudentDto dto)
        {
            if (id != dto.Id) return BadRequest("The ID in the URL does not match the body.");

            var existing = await _db.Students.FindAsync(id);
            if (existing == null) return NotFound();

            if (await _db.Students.AnyAsync(s => s.RollNo == dto.RollNo && s.Id != id))
            {
                ModelState.AddModelError(nameof(dto.RollNo), "This roll number already exists.");
                return ValidationProblem(ModelState);
            }

            existing.RollNo = dto.RollNo.Trim();
            existing.FullName = dto.FullName.Trim();
            existing.ClassName = dto.ClassName.Trim();
            existing.ParentEmail = dto.ParentEmail?.Trim();
            existing.AttendancePercent = dto.AttendancePercent;
            existing.LearningStyle = dto.LearningStyle;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Removes a student and cascades to their assessments and plans.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _db.Students.FindAsync(id);
            if (student == null) return NotFound();

            _db.Students.Remove(student);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
