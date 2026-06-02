using SportHub.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SportHub.Services.Interfaces
{
    public interface IFriendshipService
    {
        Task<Friendship?> GetFriendshipAsync(int userId1, int userId2);
        Task<List<User>> GetFriendsAsync(int userId);
        Task<List<Friendship>> GetPendingRequestsAsync(int userId);
        Task<bool> SendFriendRequestAsync(int senderId, int receiverId);
        Task<bool> AcceptFriendRequestAsync(int receiverId, int senderId);
        Task<bool> DeclineFriendRequestAsync(int receiverId, int senderId);
        Task<bool> CancelFriendRequestAsync(int senderId, int receiverId);
        Task<bool> UnfriendAsync(int userId1, int userId2);
    }
}
