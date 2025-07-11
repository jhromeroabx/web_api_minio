using System.Collections.Generic;
using System.Threading.Tasks;
using web_api_users.Application.Dtos;
using web_api_users.Controllers.ParamsDTO;

namespace web_api_users.Domain.Interfaces
{
    public interface IBucketService
    {
        Task<(bool Success, string Message)> CreateBucket(string name);
        Task<List<BucketInfo>> ListBuckets();
        Task<(bool Success, List<ObjectInfo> Objects, string Message)> ListObjects(string bucketName);
        Task<(bool Success, string Message)> DeleteBucket(string name);
    }
}