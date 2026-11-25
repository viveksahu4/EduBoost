using EduBoost.Models;
using Microsoft.EntityFrameworkCore;

namespace EduBoost.Data
{
    /// <summary>
    /// Creates demo students, assessments and a sample remedial plan the first
    /// time the application runs against an empty database. This makes the
    /// dashboard and the slow-learner detection usable straight away.
    /// </summary>
    public class DbSeeder
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DbSeeder> _log;

        public DbSeeder(ApplicationDbContext db, ILogger<DbSeeder> log)
        {
            _db = db;
            _log = log;
        }

        public async Task<bool> SeedAsync()
        {
            await EnsureDemoMethodsAsync();

            if (await _db.Students.AnyAsync())
            {
                return false;
            }

            // Subjects are normally inserted by the migration (HasData), but add
            // them defensively in case the database was created differently.
            if (!await _db.Subjects.AnyAsync())
            {
                _db.Subjects.AddRange(
                    new Subject { Name = "Mathematics" },
                    new Subject { Name = "Science" },
                    new Subject { Name = "English" });
                await _db.SaveChangesAsync();
            }

            var subjects = await _db.Subjects.OrderBy(s => s.Id).ToListAsync();
            if (subjects.Count == 0)
            {
                _log.LogWarning("No subjects available, demo seeding skipped.");
                return false;
            }

            var students = new List<Student>
            {
                new() { RollNo = "S01", FullName = "Aarav Sharma", ClassName = "8-A", ParentEmail = "aarav.parent@example.com", AttendancePercent = 92, LearningStyle = "Visual" },
                new() { RollNo = "S02", FullName = "Diya Patel", ClassName = "8-A", ParentEmail = "diya.parent@example.com", AttendancePercent = 68, LearningStyle = "Auditory" },
                new() { RollNo = "S03", FullName = "Rohan Verma", ClassName = "8-A", ParentEmail = "rohan.parent@example.com", AttendancePercent = 75, LearningStyle = "Kinesthetic" },
                new() { RollNo = "S04", FullName = "Isha Nair", ClassName = "8-B", ParentEmail = "isha.parent@example.com", AttendancePercent = 96, LearningStyle = "Reading" },
                new() { RollNo = "S05", FullName = "Kabir Singh", ClassName = "8-B", ParentEmail = "kabir.parent@example.com", AttendancePercent = 55, LearningStyle = "Kinesthetic" },
                new() { RollNo = "S06", FullName = "Meera Joshi", ClassName = "8-B", ParentEmail = "meera.parent@example.com", AttendancePercent = 88, LearningStyle = "Visual" },
                new() { RollNo = "S07", FullName = "Arjun Rao", ClassName = "9-A", ParentEmail = "arjun.parent@example.com", AttendancePercent = 62, LearningStyle = "Auditory" },
                new() { RollNo = "S08", FullName = "Sara Khan", ClassName = "9-A", ParentEmail = "sara.parent@example.com", AttendancePercent = 90, LearningStyle = "Reading" },
                new() { RollNo = "S09", FullName = "Vivaan Gupta", ClassName = "9-A", ParentEmail = "vivaan.parent@example.com", AttendancePercent = 79, LearningStyle = "Visual" },
                new() { RollNo = "S10", FullName = "Ananya Iyer", ClassName = "9-B", ParentEmail = "ananya.parent@example.com", AttendancePercent = 58, LearningStyle = "Auditory" }
            };

            _db.Students.AddRange(students);
            await _db.SaveChangesAsync();

            // Base score per student, plus a trend between the first and second exam.
            int[] baseScores = { 82, 34, 46, 91, 29, 64, 38, 76, 58, 31 };
            int[] trends = { 4, -6, 6, 2, -9, 3, 5, -2, 7, -4 };
            int[] subjectOffsets = { 0, -5, 4 };

            DateTime firstExam = DateTime.Today.AddDays(-60);
            DateTime secondExam = DateTime.Today.AddDays(-20);

            for (int i = 0; i < students.Count; i++)
            {
                for (int s = 0; s < subjects.Count; s++)
                {
                    int offset = subjectOffsets[s % subjectOffsets.Length];
                    double firstScore = Math.Clamp(baseScores[i] + offset, 8, 97);
                    double secondScore = Math.Clamp(baseScores[i] + offset + trends[i], 8, 97);

                    _db.Assessments.Add(new Assessment
                    {
                        StudentId = students[i].Id,
                        SubjectId = subjects[s].Id,
                        ExamType = "Unit Test 1",
                        MarksObtained = firstScore,
                        MaxMarks = 100,
                        ExamDate = firstExam
                    });

                    _db.Assessments.Add(new Assessment
                    {
                        StudentId = students[i].Id,
                        SubjectId = subjects[s].Id,
                        ExamType = "Mid Term",
                        MarksObtained = secondScore,
                        MaxMarks = 100,
                        ExamDate = secondExam
                    });
                }
            }

            await _db.SaveChangesAsync();

            // Sample remedial plans with progress logs across subjects, VARK styles,
            // AI-generated flags, and Active/Completed statuses.
            var diya = students[1];
            var rohan = students[2];
            var kabir = students[4];
            var arjun = students[6];
            var ananya = students[9];

            var math = subjects[0];
            var science = subjects.Count > 1 ? subjects[1] : subjects[0];
            var english = subjects.Count > 2 ? subjects[2] : subjects[0];

            var diyaPlan = new RemedialPlan
            {
                StudentId = diya.Id,
                SubjectId = math.Id,
                Title = "Mathematics foundation plan",
                Strategy =
                    "Week 1: Revise multiplication tables and fractions using visual charts.\n" +
                    "Week 2: Daily 15-minute practice with a peer tutor.\n" +
                    "Week 3: Gamified quiz on fractions and decimals.\n" +
                    "Week 4: Re-test and discuss progress with parents.",
                IsAiGenerated = false,
                Status = "Active",
                StartDate = DateTime.Today.AddDays(-14)
            };

            var kabirPlan = new RemedialPlan
            {
                StudentId = kabir.Id,
                SubjectId = science.Id,
                Title = "AI Plan - Hands-On Science Lab Recovery",
                Strategy =
                    "Week 1: Build 3D clay & block models for core cell and matter concepts.\n" +
                    "Week 2: Classroom movement stations with 10-minute tactile mini-experiments.\n" +
                    "Week 3: Gamified team quiz relay on weak chapters.\n" +
                    "Week 4: Practical demonstration re-test and parent attendance check-in.",
                IsAiGenerated = true,
                Status = "Active",
                StartDate = DateTime.Today.AddDays(-10)
            };

            var ananyaPlan = new RemedialPlan
            {
                StudentId = ananya.Id,
                SubjectId = math.Id,
                Title = "AI Plan - Auditory Algebra & Equations Boost",
                Strategy =
                    "Week 1: Think-aloud problem walkthroughs and recorded audio mnemonics for formulas.\n" +
                    "Week 2: Structured peer-tutoring discussion pairs for linear equations.\n" +
                    "Week 3: Oral rapid-fire equation sprints and verbal error analysis.\n" +
                    "Week 4: Cumulative post-assessment and parent study-routine review.",
                IsAiGenerated = true,
                Status = "Active",
                StartDate = DateTime.Today.AddDays(-12)
            };

            var arjunPlan = new RemedialPlan
            {
                StudentId = arjun.Id,
                SubjectId = english.Id,
                Title = "Oral Reading & Grammar Fluency Plan",
                Strategy =
                    "Week 1: Guided read-aloud comprehension circles with audio story prompts.\n" +
                    "Week 2: Peer storytelling and Socratic vocabulary building.\n" +
                    "Week 3: Sentence-framing verbal games and short dictation drills.\n" +
                    "Week 4: Oral fluency check and written comprehension post-test.",
                IsAiGenerated = false,
                Status = "Active",
                StartDate = DateTime.Today.AddDays(-7)
            };

            var rohanPlan = new RemedialPlan
            {
                StudentId = rohan.Id,
                SubjectId = science.Id,
                Title = "Experiential Physics & Lab Concept Mastery",
                Strategy =
                    "Week 1: Tactile circuit and force kits to reinforce foundational laws.\n" +
                    "Week 2: Role-play concept enactments and lab station worksheets.\n" +
                    "Week 3: Interactive gamified quiz challenges on motion and energy.\n" +
                    "Week 4: Final post-remedial evaluation and mastery sign-off.",
                IsAiGenerated = true,
                Status = "Completed",
                StartDate = DateTime.Today.AddDays(-35)
            };

            _db.RemedialPlans.AddRange(diyaPlan, kabirPlan, ananyaPlan, arjunPlan, rohanPlan);
            await _db.SaveChangesAsync();

            _db.ProgressLogs.AddRange(
                new ProgressLog
                {
                    RemedialPlanId = diyaPlan.Id,
                    LogDate = DateTime.Today.AddDays(-7),
                    Notes = "Completed fractions revision and peer-tutoring sessions.",
                    ScoreAfter = 40
                },
                new ProgressLog
                {
                    RemedialPlanId = diyaPlan.Id,
                    LogDate = DateTime.Today.AddDays(-2),
                    Notes = "Fractions quiz improved from 34% to 44%.",
                    ScoreAfter = 44
                },
                new ProgressLog
                {
                    RemedialPlanId = kabirPlan.Id,
                    LogDate = DateTime.Today.AddDays(-6),
                    Notes = "Built 3D cell structure model; engagement improved noticeably.",
                    ScoreAfter = 34
                },
                new ProgressLog
                {
                    RemedialPlanId = kabirPlan.Id,
                    LogDate = DateTime.Today.AddDays(-2),
                    Notes = "Scored 41% on hands-on lab station check.",
                    ScoreAfter = 41
                },
                new ProgressLog
                {
                    RemedialPlanId = ananyaPlan.Id,
                    LogDate = DateTime.Today.AddDays(-7),
                    Notes = "Practiced verbal step-by-step solving in peer pairs.",
                    ScoreAfter = 39
                },
                new ProgressLog
                {
                    RemedialPlanId = ananyaPlan.Id,
                    LogDate = DateTime.Today.AddDays(-1),
                    Notes = "Oral equation sprint score reached 46%.",
                    ScoreAfter = 46
                },
                new ProgressLog
                {
                    RemedialPlanId = arjunPlan.Id,
                    LogDate = DateTime.Today.AddDays(-3),
                    Notes = "Completed first read-aloud comprehension circle.",
                    ScoreAfter = 48
                },
                new ProgressLog
                {
                    RemedialPlanId = rohanPlan.Id,
                    LogDate = DateTime.Today.AddDays(-25),
                    Notes = "Week 1 tactile circuit lab completed.",
                    ScoreAfter = 50
                },
                new ProgressLog
                {
                    RemedialPlanId = rohanPlan.Id,
                    LogDate = DateTime.Today.AddDays(-15),
                    Notes = "Gamified physics quiz score jumped to 58%.",
                    ScoreAfter = 58
                },
                new ProgressLog
                {
                    RemedialPlanId = rohanPlan.Id,
                    LogDate = DateTime.Today.AddDays(-5),
                    Notes = "Post-remedial evaluation passed with 65%; plan completed.",
                    ScoreAfter = 65
                });

            await _db.SaveChangesAsync();
            _log.LogInformation("Seeded {Students} students, {Assessments} assessments and 5 remedial plans.",
                students.Count, students.Count * subjects.Count * 2);

            return true;
        }

        private async Task EnsureDemoMethodsAsync()
        {
            var demoMethods = new List<InnovativeMethod>
            {
                new()
                {
                    Name = "Color-Coded Concept Anchor Charts",
                    Category = "Visual Scaffolding",
                    SuitableLearningStyle = "Visual",
                    Description = "Demo: Divide the board into 3 colors—Blue for definitions, Green for solved steps, Orange for common mistakes. Example: Draw step-by-step fraction bar models so visual learners spot patterns at a glance."
                },
                new()
                {
                    Name = "Think-Aloud Audio Walkthroughs",
                    Category = "Oral Modeling",
                    SuitableLearningStyle = "Auditory",
                    Description = "Demo: Solve a problem aloud narrating every 'why' behind each step, then have pairs explain it back in 3 minutes. Example: Share 2-minute voice recaps of Science formulas or Algebra rules."
                },
                new()
                {
                    Name = "Socratic Questioning Circles",
                    Category = "Discussion",
                    SuitableLearningStyle = "Auditory",
                    Description = "Demo: Seat a small group in a circle and guide discovery with 'What happens if...' prompts instead of lecturing. Example: Let struggling learners build English grammar or History answers verbally first."
                },
                new()
                {
                    Name = "Tactile 3D Models & Manipulatives",
                    Category = "Hands-On Lab",
                    SuitableLearningStyle = "Kinesthetic",
                    Description = "Demo: Use clay, paper cutouts, blocks, or fraction strips so students physically build concepts. Example: Fold/cut paper circles for fractions or assemble 3D cell/molecular models before textbook drills."
                },
                new()
                {
                    Name = "Learning Stations Rotation (10-Min Circuit)",
                    Category = "Active Classroom",
                    SuitableLearningStyle = "Kinesthetic",
                    Description = "Demo: Set up 4 corners—1) Mini-experiment, 2) Flashcard match, 3) Board problem, 4) Teacher check-in. Groups rotate every 8–10 minutes to keep low-attention learners engaged through movement."
                },
                new()
                {
                    Name = "Cornell Guided Note Templates",
                    Category = "Structured Notes",
                    SuitableLearningStyle = "Reading",
                    Description = "Demo: Hand out a 3-section sheet: Cue Keywords (left), Short Notes (right), and 2-line Summary (bottom). Example: Students fill missing key terms during a 15-minute recap to build crisp revision sheets."
                },
                new()
                {
                    Name = "One-Minute Exit Ticket & Cheat-Sheet",
                    Category = "Formative Writing",
                    SuitableLearningStyle = "Reading",
                    Description = "Demo: In the last 5 minutes of class, students write '1 thing I learned + 1 doubt I still have' on a card. Review cards to plan next-day 10-minute targeted remedial drills."
                }
            };

            var existingNames = await _db.InnovativeMethods
                .Select(m => m.Name)
                .ToListAsync();

            var toAdd = demoMethods
                .Where(m => !existingNames.Contains(m.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (toAdd.Count > 0)
            {
                _db.InnovativeMethods.AddRange(toAdd);
                await _db.SaveChangesAsync();
                _log.LogInformation("Seeded {Count} demo innovative teaching methods.", toAdd.Count);
            }
        }
    }
}
