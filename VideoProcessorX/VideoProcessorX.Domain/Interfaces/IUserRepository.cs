using VideoProcessorX.Domain.Entities;

namespace VideoProcessorX.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetByUsernameAsync(string username);
        Task CreateAsync(User user);
    }
}
