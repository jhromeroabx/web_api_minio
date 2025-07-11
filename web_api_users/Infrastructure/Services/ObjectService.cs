using Microsoft.AspNetCore.Http;
using Minio;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using web_api_users.Application.Interfaces;
using web_api_users.Domain.Interfaces;

namespace web_api_users.Infrastructure.Services
{
    public class ObjectService : IObjectService
    {
        private readonly IFileManager _fileManager;

        public ObjectService(IFileManager fileManager)
        {
            _fileManager = fileManager;
        }

        // Subir cualquier tipo de objeto
        public async Task<(bool Success, string Message)> UploadObjectAsync(string bucket, string objectName, string contentType, IFormFile file)
        {
            if (file.Length <= 0)
                return (false, "Archivo vacío.");

            try
            {
                var minio = _fileManager.GetMinio();

                // Verificar si el objeto ya existe
                await minio.StatObjectAsync(new StatObjectArgs().WithBucket(bucket).WithObject(objectName));
                return (false, $"El objeto '{objectName}' ya existe.");
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                var imageTypes = new List<string>
        {
            "image/apng", "image/avif", "image/jpeg", "image/png", "image/svg+xml", "image/webp"
        };

                await using var fileStream = new MemoryStream();
                await file.CopyToAsync(fileStream);
                fileStream.Position = 0;

                Stream finalStream = fileStream;

                if (imageTypes.Contains(contentType))
                {
                    fileStream.Position = 0;
                    var image = Image.Load(fileStream);

                    if (image.Width > 1200 || image.Height > 600)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(1200, 600),
                            Mode = ResizeMode.Max
                        }));

                        var resizedStream = new MemoryStream();
                        image.Save(resizedStream, new JpegEncoder());
                        resizedStream.Position = 0;
                        finalStream = resizedStream;
                    }
                }

                await ProcessAndStoreObject(bucket, objectName, contentType, finalStream);

                var tipo = contentType.StartsWith("image") ? "Imagen" :
                           contentType.StartsWith("audio") ? "Audio" :
                           contentType.StartsWith("application/pdf") ? "PDF" :
                           "Archivo";

                return (true, $"¡{tipo} subido exitosamente!");
            }
            catch (Exception ex)
            {
                return (false, $"Error al subir objeto: {ex.Message}");
            }
        }


        // Obtener cualquier tipo de objeto desde Minio
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

                return (true, buffer, contentType, "Objeto recuperado correctamente. :D");
            }
            catch (Exception ex)
            {
                return (false, null, null, $"Error al obtener el objeto: {ex.Message}");
            }
        }

        // Eliminar un objeto específico de un bucket
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

        // Método privado para almacenar el objeto en Minio
        private async Task ProcessAndStoreObject(string bucket, string objectName, string contentType, Stream stream)
        {
            var minio = _fileManager.GetMinio();

            // Reinicia la posición del stream antes de enviarlo a MinIO
            stream.Position = 0;

            var args = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType)
                .WithServerSideEncryption(null);

            await minio.PutObjectAsync(args);
        }
    }
}