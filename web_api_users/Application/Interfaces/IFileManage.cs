using Minio;

namespace web_api_users.Application.Interfaces
{
    public interface IFileManager
    {
        MinioClient GetMinio();
    }
}
