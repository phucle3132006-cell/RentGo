using CarRental.DAL.Context;
using CarRental.DAL.Interfaces;
using CarRental.Models.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CarRental.DAL.Repositories
{
    // Triển khai truy xuất dữ liệu bảng users bằng EF Core.
    public class UserRepository : IUserRepository
    {
        private readonly CarRentalDbContext _context;

        // DbContext được cung cấp thông qua Dependency Injection.
        public UserRepository(CarRentalDbContext context)
        {
            _context = context;
        }

        // Lấy thông tin người dùng theo ID.
        public async Task<User?> GetByIdAsync(long id)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        // Tìm người dùng theo email.
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public Task<bool> IsBlacklistedAsync(string email, string phone) =>
            _context.IsBlacklistedAsync(email, phone);

        public async Task CreateAsync(User user)
        {
            await using var transaction = await _context.BeginUserIdentityTransactionAsync();
            if (await IsBlacklistedAsync(user.Email, user.Phone))
                throw new InvalidOperationException("Email hoặc số điện thoại đang bị chặn đăng ký.");
            if (await _context.Users.AnyAsync(item => item.Email == user.Email))
                throw new InvalidOperationException("Email đã được sử dụng.");
            if (await _context.Users.AnyAsync(item => item.Phone == user.Phone))
                throw new InvalidOperationException("Số điện thoại đã được sử dụng.");

            _context.Users.Add(user);
            try
            {
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
            {
                throw new InvalidOperationException("Email hoặc số điện thoại đã được sử dụng.", error);
            }
        }
    }
}
