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

        //public void SetupMinio(MinioClient minio)
        //{
        //    if (this.minio == null)
        //        this.minio = minio;
        //}

        //public void SetupMinioHard()
        //{
        //    if (this.minio == null)
        //        this.minio = new MinioClient()
        //                            .WithEndpoint("192.168.18.6:8500")
        //                            .WithCredentials("loasi.wastore",
        //                                     "loasi.wastore@wasd12125")
        //                            .Build();
        //}
    }
}
