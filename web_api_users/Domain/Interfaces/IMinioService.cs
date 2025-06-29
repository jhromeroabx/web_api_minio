using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using web_api_users.Application.Dtos;
using web_api_users.Controllers.ParamsDTO;

namespace web_api_users.Domain.Interfaces
{
    public interface IMinioService
    {
        Task<(bool Success, string Message)> CreateBucket(string name);
        Task<List<BucketInfo>> ListBuckets();
        Task<(bool Success, List<ObjectInfo> Objects, string Message)> ListObjects(string bucketName);
        Task<(bool Success, string Message)> DeleteBucket(string name);
        Task<(bool Success, string Message)> UploadImageAsync(string bucket, string objectName, string contentType, IFormFile file);
        Task<(bool Success, string Message)> UploadAudioAsync(string bucket, string objectName, string contentType, IFormFile file);
        Task<(bool Success, byte[] Data, string ContentType, string Message)> GetAudioAsync(string bucket, string objectName);
        Task<(bool Success, byte[] Data, string ContentType, string Message)> GetObjectAsync(string bucket, string objectName);
        Task<(bool Success, string Message)> DeleteObjectAsync(string bucket, string objectName);
    }
}
