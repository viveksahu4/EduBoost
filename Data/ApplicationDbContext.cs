using EduBoost.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EduBoost.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Assessment> Assessments => Set<Assessment>();
        public DbSet<RemedialPlan> RemedialPlans => Set<RemedialPlan>();
        public DbSet<InnovativeMethod> InnovativeMethods => Set<InnovativeMethod>();
        public DbSet<ProgressLog> ProgressLogs => Set<ProgressLog>();
        public DbSet<StudentPerformanceReport> StudentPerformanceReports => Set<StudentPerformanceReport>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Percentage is a calculated property, so it must not become a column.
            b.Entity<Assessment>().Ignore(a => a.Percentage);

            // Map database reporting view
            b.Entity<StudentPerformanceReport>()
                .HasNoKey()
                .ToView("vw_StudentPerformance");

            b.Entity<Student>()
                .HasIndex(s => s.RollNo)
                .IsUnique();

            b.Entity<Subject>()
                .HasIndex(s => s.Name)
                .IsUnique();

            b.Entity<RemedialPlan>()
                .HasOne(p => p.Student)
                .WithMany(s => s.RemedialPlans)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<ProgressLog>()
                .HasOne(l => l.RemedialPlan)
                .WithMany(p => p.Logs)
                .HasForeignKey(l => l.RemedialPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<Subject>().HasData(
                new Subject { Id = 1, Name = "Mathematics" },
                new Subject { Id = 2, Name = "Science" },
                new Subject { Id = 3, Name = "English" });

            b.Entity<InnovativeMethod>().HasData(
                new InnovativeMethod { Id = 1, Name = "Peer Tutoring", Category = "Collaborative", SuitableLearningStyle = "Auditory", Description = "Strong student explains concepts to a weaker partner." },
                new InnovativeMethod { Id = 2, Name = "Gamified Quizzes", Category = "Technology", SuitableLearningStyle = "Kinesthetic", Description = "Points, badges and levels for practice questions." },
                new InnovativeMethod { Id = 3, Name = "Mind Maps", Category = "Visual", SuitableLearningStyle = "Visual", Description = "Colourful diagrams linking ideas of a chapter." },
                new InnovativeMethod { Id = 4, Name = "Storytelling", Category = "Narrative", SuitableLearningStyle = "Reading", Description = "Teach concepts through short stories and real-life examples." },
                new InnovativeMethod { Id = 5, Name = "Flipped Classroom", Category = "Blended", SuitableLearningStyle = "Visual", Description = "Watch a short video at home, practise in class." });
        }
    }
}
