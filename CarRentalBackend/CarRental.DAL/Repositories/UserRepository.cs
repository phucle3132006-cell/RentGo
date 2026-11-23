using CarRental.DAL.Context;
using CarRental.DAL.Interfaces;
using CarRental.Models.Entities;
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

        // Kiểm tra email đã được sử dụng hay chưa.
        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email);
        }

        // Kiểm tra số điện thoại đã được sử dụng hay chưa.
        public async Task<bool> PhoneExistsAsync(string phone)
        {
            return await _context.Users
                .AnyAsync(u => u.Phone == phone);
        }

        // Thêm người dùng vào DbContext.
        // Chưa ghi xuống database ở bước này.
        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        // Ghi các thay đổi xuống SQL Server.
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}