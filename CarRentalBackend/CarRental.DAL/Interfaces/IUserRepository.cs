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

        // Kiểm tra email đã tồn tại hay chưa.
        Task<bool> EmailExistsAsync(string email);

        // Kiểm tra số điện thoại đã tồn tại hay chưa.
        Task<bool> PhoneExistsAsync(string phone);

        // Thêm người dùng mới.
        Task AddAsync(User user);

        // Lưu các thay đổi xuống database.
        Task<int> SaveChangesAsync();
    }
}