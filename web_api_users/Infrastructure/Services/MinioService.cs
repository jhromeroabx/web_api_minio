using Minio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_api_users.Application.Dtos;
using web_api_users.Application.Interfaces;
using web_api_users.Controllers.ParamsDTO;
using web_api_users.Domain.Interfaces;

namespace web_api_users.Infrastructure.Services
{
    public class MinioService : IMinioService
    {
        private readonly IFileManager _fileManager;

        public MinioService(IFileManager fileManager)
        {
            _fileManager = fileManager;
        }

        public async Task<(bool Success, string Message)> CreateBucket(string name)
        {
            try
            {
                BucketExistsArgs bucketExistsArgs = new BucketExistsArgs().WithBucket(name);

                bool found = await _fileManager.GetMinio().BucketExistsAsync(bucketExistsArgs);
                if (!found)
                {
                    MakeBucketArgs makeBucketArgs = new MakeBucketArgs()
                        .WithBucket(name)
                        .WithLocation("/Data");

                    await _fileManager.GetMinio().MakeBucketAsync(makeBucketArgs);
                }
                else
                {
                    return (false, $"No se creo el bucket, YA EXISTE: {name}");
                }

                return (true, $"Se creo el bucket: {name}");
            }
            catch (Exception ex)
            {
                return (false, $"No se creo el bucket: {ex.Message}");
            }
        }

        public async Task<List<BucketInfo>> ListBuckets()
        {
            try
            {
                var minio = _fileManager.GetMinio();
                var response = await minio.ListBucketsAsync();

                var listBuckets = response.Buckets.Select(b => new BucketInfo
                {
                    Name = b.Name,
                    CreationDate = b.CreationDateDateTime
                }).ToList();

                return listBuckets;
            }
            catch (Exception ex) { 
                return null;
            }
           
        }

        
        public async Task<(bool Success, string Message)> DeleteBucket(string name)
        {
            try
            {
                var minio = _fileManager.GetMinio();
                bool found = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(name));

                if (!found)
                    return (false, "El bucket no existe");

                await minio.RemoveBucketAsync(new RemoveBucketArgs().WithBucket(name));

                return (true, $"Se borró el bucket '{name}'");
            }
            catch (Exception ex)
            {
                return (false, $"Error al eliminar bucket: {ex.Message}");
            }
        }

       

        public async Task<(bool Success, List<ObjectInfo> Objects, string Message)> ListObjects(string bucketName)
        {
            try
            {
                var minio = _fileManager.GetMinio();
                var exists = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
                if (!exists)
                    return (false, null, "El bucket no existe");

                var objects = new List<ObjectInfo>();
                var observable = minio.ListObjectsAsync(new ListObjectsArgs().WithBucket(bucketName).WithRecursive(true));
                var completionSource = new TaskCompletionSource();

                observable.Subscribe(
                    item => objects.Add(new ObjectInfo
                    {
                        Key = item.Key,
                        LastModifiedDateTime = item.LastModifiedDateTime
                    }),
                    ex =>
                    {
                        completionSource.SetException(ex);
                    },
                    () => completionSource.SetResult()
                );

                await completionSource.Task;

                return (true, objects, "Objetos listados correctamente");
            }
            catch (Exception ex) {
                return (false, null, $"Error al listar objetcts: {ex.Message}");
            }
            
        }
    }
}
