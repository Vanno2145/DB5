using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EFCoreStoredProcedures
{
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int Age { get; set; }

        [ForeignKey("Company")]
        public int? CompanyId { get; set; }
        public Company Company { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Company> Companies { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=TestDB;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Company>().ToTable("Companies");
        }

        public async Task<List<UserWithCompanyDto>> GetUsersWithCompaniesAsync()
        {
            return await Users.FromSqlRaw("EXEC GetUsersWithCompanies").Select(u => new UserWithCompanyDto
            {
                Id = u.Id,
                Name = u.Name,
                Age = u.Age,
                CompanyId = u.CompanyId,
                CompanyName = u.Company != null ? u.Company.Name : null
            }).ToListAsync();
        }

        public async Task<List<User>> GetUsersByNamePatternAsync(string namePattern)
        {
            return await Users.FromSqlInterpolated($"EXEC GetUsersByNamePattern {namePattern}").ToListAsync();
        }

        public async Task<double> GetAverageUserAgeAsync()
        {
            var avgAgeParam = new SqlParameter
            {
                ParameterName = "@AvgAge",
                SqlDbType = System.Data.SqlDbType.Float,
                Direction = System.Data.ParameterDirection.Output
            };

            await Database.ExecuteSqlRawAsync("EXEC GetAverageUserAge @AvgAge OUTPUT", avgAgeParam);
            return (double)(avgAgeParam.Value ?? 0);
        }
    }

    public class UserWithCompanyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public int? CompanyId { get; set; }
        public string CompanyName { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            using var context = new AppDbContext();
            await context.Database.EnsureCreatedAsync();

            // Вызов 1: Получение пользователей с их компаниями
            var usersWithCompanies = await context.GetUsersWithCompaniesAsync();
            Console.WriteLine("Users with Companies:");
            foreach (var user in usersWithCompanies)
            {
                Console.WriteLine($"{user.Name} (Age: {user.Age}) - Company: {user.CompanyName ?? "None"}");
            }

            // Вызов 2: Поиск пользователей по имени
            var usersNamedTom = await context.GetUsersByNamePatternAsync("Tom");
            Console.WriteLine("\nUsers with name like 'Tom':");
            foreach (var user in usersNamedTom)
            {
                Console.WriteLine($"{user.Name} (Age: {user.Age})");
            }

            // Вызов 3: Получение среднего возраста
            double avgAge = await context.GetAverageUserAgeAsync();
            Console.WriteLine($"\nAverage age of users: {avgAge}");
        }
    }
}
