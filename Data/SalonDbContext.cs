using Hairdresser_Website.Models;
using Microsoft.EntityFrameworkCore;

namespace Hairdresser_Website.Data
{
    public class SalonDbContext : DbContext
    {
        public SalonDbContext(DbContextOptions<SalonDbContext> options) : base(options) { }


        public DbSet<Salon> Salons { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<EmployeeAvailability> EmployeeAvailability { get; set; } 
        public DbSet<SalonWorkingHours> SalonWorkingHours { get; set; }
        public DbSet<UnavailableSlot> UnavailableSlots { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {



            modelBuilder.Entity<User>()
                .HasKey(u => u.UserId); 

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.User) 
                .WithMany(u => u.Appointments) 
                .HasForeignKey(a => a.UserId) 
                .OnDelete(DeleteBehavior.Cascade); 


            modelBuilder.Entity<Service>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<User>()
            .HasKey(u => u.UserId);
        }
    }
}