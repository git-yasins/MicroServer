using System.Threading.Tasks;
using Recommend.API.Dtos;

namespace Recommend.API.Services
{
    public interface IUserService
    {
        /// <summary>
        /// 获取用户基本信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
         Task<UserIdentity> GetBaseUserInfoAsync(int userId);
    }
}