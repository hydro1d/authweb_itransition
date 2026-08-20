using System;
using System.Threading.Tasks;
using AuthWeb.Data;
using AuthWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthWeb.Tests
{
    public class UserDatabaseTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public UserDatabaseTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = new ApplicationDbContext(_options);
            context.Database.EnsureCreated();
        }

        [Fact]
        public async Task DuplicateEmail_ThrowsUniqueConstraintViolation()
        {
            using var context = new ApplicationDbContext(_options);
            
            var user1 = new User
            {
                Id = Guid.NewGuid(),
                Name = "First User",
                Email = "duplicate@example.com",
                PasswordHash = "hash1",
                Status = UserStatus.Active
            };

            context.Users.Add(user1);
            await context.SaveChangesAsync();

            var user2 = new User
            {
                Id = Guid.NewGuid(),
                Name = "Second User",
                Email = "duplicate@example.com", // Duplicate email
                PasswordHash = "hash2",
                Status = UserStatus.Active
            };

            context.Users.Add(user2);
            
            // Assert that saving duplicate email throws DbUpdateException due to unique index
            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await context.SaveChangesAsync();
            });
        }

        [Fact]
        public async Task HardDelete_AllowsReRegistrationWithSameEmail()
        {
            using var context = new ApplicationDbContext(_options);

            var email = "reused@example.com";
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "Original User",
                Email = email,
                PasswordHash = "hash",
                Status = UserStatus.Active
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Hard delete user
            context.Users.Remove(user);
            await context.SaveChangesAsync();

            // Re-register with same email
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Name = "New User",
                Email = email,
                PasswordHash = "hash2",
                Status = UserStatus.Unverified
            };

            context.Users.Add(newUser);
            var resultCount = await context.SaveChangesAsync();

            Assert.Equal(1, resultCount);
        }

        [Fact]
        public void PasswordHasher_AcceptsSingleCharacterPassword()
        {
            var hasher = new PasswordHasher<User>();
            var user = new User();

            var singleCharPassword = "a";
            var hash = hasher.HashPassword(user, singleCharPassword);
            var result = hasher.VerifyHashedPassword(user, hash, singleCharPassword);

            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        public void Dispose()
        {
            _connection.Close();
        }
    }
}
