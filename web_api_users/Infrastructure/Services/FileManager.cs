using Minio;
using web_api_users.Application.Interfaces;
using Microsoft.Extensions.Options;
using web_api_users.Application.Dtos;

namespace web_api_users.Infrastructure.Services
{
    public class FileManager : IFileManager
    {
        MinioClient minio = null;

        public FileManager(IOptions<CredentialsMINio> options)
        {
            var config = options.Value;

            minio = new MinioClient()
                .WithEndpoint(config.endpoint)
                .WithCredentials(config.accessKey, config.secretKey)
                .Build();
        }

        public MinioClient GetMinio()
        {
            return minio;
        }
    }
}
