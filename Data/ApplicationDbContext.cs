using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuanLyKTX.Models;

namespace QuanLyKTX.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Apartment> Apartments { get; set; }
        public DbSet<ResidentProfile> ResidentProfiles { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<ResidentCard> ResidentCards { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceBooking> ServiceBookings { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Hồ sơ đăng ký ban đầu của sinh viên chưa ở KTX
        public DbSet<StudentRegistration> StudentRegistrations { get; set; }
        public DbSet<StudentRegistrationDocument> StudentRegistrationDocuments { get; set; }

        // Chức năng sinh viên sau khi đăng nhập
        public DbSet<StudentRequestForm> StudentRequestForms { get; set; }
        public DbSet<HealthInsuranceDeclaration> HealthInsuranceDeclarations { get; set; }
        public DbSet<MedicalRegistration> MedicalRegistrations { get; set; }
        public DbSet<SurveyResponse> SurveyResponses { get; set; }
        public DbSet<FormTemplate> FormTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ResidentProfile>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Invoice>()
                .Property(x => x.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ResidentCard>()
                .HasIndex(x => x.CardCode)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .Property(x => x.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Service>()
                .Property(x => x.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Notification>()
                .Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<StudentRegistration>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentRegistration>()
                .HasMany(x => x.Documents)
                .WithOne(x => x.StudentRegistration)
                .HasForeignKey(x => x.StudentRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentRegistration>()
                .HasIndex(x => x.Username);

            modelBuilder.Entity<StudentRegistration>()
                .HasIndex(x => x.StudentCode);

            modelBuilder.Entity<StudentRequestForm>()
                .HasOne(x => x.ResidentProfile)
                .WithMany()
                .HasForeignKey(x => x.ResidentProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HealthInsuranceDeclaration>()
                .HasOne(x => x.ResidentProfile)
                .WithMany()
                .HasForeignKey(x => x.ResidentProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MedicalRegistration>()
                .HasOne(x => x.ResidentProfile)
                .WithMany()
                .HasForeignKey(x => x.ResidentProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SurveyResponse>()
                .HasOne(x => x.ResidentProfile)
                .WithMany()
                .HasForeignKey(x => x.ResidentProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Apartment>()
                .Property(x => x.RoomPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<FormTemplate>()
                .HasIndex(x => x.Type);

            modelBuilder.Entity<FormTemplate>()
                .Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        }
    }
}
