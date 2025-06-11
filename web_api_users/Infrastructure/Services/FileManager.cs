using Minio;
using web_api_users.Application.Interfaces;

namespace web_api_users.Infrastructure.Services
{
    public class FileManager : IFileManager
    {
        MinioClient minio = null;

        public FileManager()
        {
            minio = new MinioClient()
                                    .WithEndpoint("192.168.1.139:8530")
                                    .WithCredentials("123wasd",
                                             "123wasd@wasd")
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
