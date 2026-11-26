using CarRental.Models.Entities;

namespace CarRental.DAL.Interfaces
{
    // Định nghĩa các thao tác truy xuất dữ liệu người dùng.
    public interface IUserRepository
    {
        // Tìm người dùng theo ID.
        Task<User?> GetByIdAsync(long id);

        // Tìm người dùng theo email.
        Task<User?> GetByEmailAsync(string email);

        Task<bool> IsBlacklistedAsync(string email, string phone);

        Task CreateAsync(User user);
    }
}
