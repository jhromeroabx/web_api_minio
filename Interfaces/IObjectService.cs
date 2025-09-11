using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace web_api_users.Domain.Interfaces
{
    public interface IObjectService
    {
        Task<(bool Success, string Message)> UploadObjectAsync(string bucket, string objectName, string contentType, IFormFile file);
        Task<(bool Success, byte[] Data, string ContentType, string Message)> GetObjectAsync(string bucket, string objectName);
        Task<(bool Success, string Message)> DeleteObjectAsync(string bucket, string objectName);
    }
}
