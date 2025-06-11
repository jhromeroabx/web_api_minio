using Minio;

namespace web_api_users.Application.Interfaces
{
    public interface IFileManager
    {
        //void SetupMinio(MinioClient minio);
        //void SetupMinioHard();
        MinioClient GetMinio();
    }
}
