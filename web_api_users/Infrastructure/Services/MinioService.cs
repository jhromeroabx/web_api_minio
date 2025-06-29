using Microsoft.AspNetCore.Http;
using Minio;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
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

        public async Task<(bool Success, string Message)> UploadImageAsync(string bucket, string objectName, string contentType, IFormFile file)
        {
            if (file.Length <= 0)
                return (false, "Archivo vacío.");

            try
            {
                var minio = _fileManager.GetMinio();

                // Validar si ya existe el objeto
                await minio.StatObjectAsync(new StatObjectArgs().WithBucket(bucket).WithObject(objectName));
                return (false, $"El objeto '{objectName}' ya existe.");
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                List<string> allowedTypes = new()
                {
                    "image/apng", "image/avif", "image/jpeg", "image/png", "image/svg+xml", "image/webp"
                };

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                Stream finalStream = memoryStream;

                if (allowedTypes.Contains(contentType))
                {
                    var image = Image.Load(memoryStream);
                    if (image.Width > 1200 || image.Height > 600)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(1200, 600),
                            Mode = ResizeMode.Max
                        }));

                        var resized = new MemoryStream();
                        image.Save(resized, new JpegEncoder());
                        resized.Position = 0;

                        finalStream = resized;
                    }
                }

                await ProcessAndStoreImage(bucket, objectName, contentType, finalStream);
                return (true, "¡Imagen creada exitosamente!");
            }
            catch (Exception ex)
            {
                return (false, $"Error al subir imagen: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UploadAudioAsync(string bucket, string objectName, string contentType, IFormFile file)
        {
            if (file.Length <= 0)
                return (false, "Archivo vacío.");

            try
            {
                var minio = _fileManager.GetMinio();
                await minio.StatObjectAsync(new StatObjectArgs().WithBucket(bucket).WithObject(objectName));
                return (false, $"El objeto '{objectName}' ya existe.");
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                await ProcessAndStoreImage(bucket, objectName, contentType, memoryStream);
                return (true, "¡Audio creado exitosamente!");
            }
            catch (Exception ex)
            {
                return (false, $"Error al subir audio: {ex.Message}");
            }
        }


        public async Task<(bool Success, byte[] Data, string ContentType, string Message)> GetAudioAsync(string bucket, string objectName)
        {
            try
            {
                var minio = _fileManager.GetMinio();
                await minio.StatObjectAsync(new StatObjectArgs().WithBucket(bucket).WithObject(objectName));

                byte[] data = null;

                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectName)
                    .WithCallbackStream(stream =>
                    {
                        using var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        data = ms.ToArray();
                    });

                await minio.GetObjectAsync(getObjectArgs);

                return (true, data, "audio/mpeg", "Éxito");
            }
            catch (Exception ex)
            {
                return (false, null, null, $"Error al obtener audio: {ex.Message}");
            }
        }

        private async Task ProcessAndStoreImage(string bucket, string objectName, string contentType, Stream stream)
        {
            var minio = _fileManager.GetMinio();

            var args = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType)
                .WithServerSideEncryption(null);

            await minio.PutObjectAsync(args);
        }

        public async Task<(bool Success, byte[] Data, string ContentType, string Message)> GetObjectAsync(string bucket, string objectName)
        {
            try
            {
                var minio = _fileManager.GetMinio();

                await minio.StatObjectAsync(new StatObjectArgs().WithBucket(bucket).WithObject(objectName));

                byte[] buffer = null;
                string contentType = null;

                var args = new GetObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectName)
                    .WithCallbackStream(stream =>
                    {
                        using var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        buffer = ms.ToArray();
                    });

                var result = await minio.GetObjectAsync(args);
                contentType = result.ContentType;

                return (true, buffer, contentType, "Objeto recuperado correctamente.");
            }
            catch (Exception ex)
            {
                return (false, null, null, $"Error al obtener el objeto: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteObjectAsync(string bucket, string objectName)
        {
            try
            {
                var minio = _fileManager.GetMinio();

                await minio.StatObjectAsync(new StatObjectArgs().WithBucket(bucket).WithObject(objectName));

                var rmArgs = new RemoveObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectName);

                await minio.RemoveObjectAsync(rmArgs);

                return (true, $"Objeto '{objectName}' eliminado correctamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Error al eliminar el objeto '{objectName}': {ex.Message}");
            }
        }
    }
}
