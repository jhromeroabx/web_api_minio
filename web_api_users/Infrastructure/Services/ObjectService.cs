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
using Microsoft.Extensions.Logging;

namespace web_api_users.Infrastructure.Services
{
    public class ObjectService : IObjectService
    {
        private readonly IFileManager _fileManager;
        private readonly ILogger<ObjectService> _logger;

        public ObjectService(IFileManager fileManager, ILogger<ObjectService> logger)
        {
            _fileManager = fileManager;
            _logger = logger;
        }

        // Subir cualquier tipo de objeto - ✅ CORREGIDO: Permite reemplazo
        public async Task<(bool Success, string Message)> UploadObjectAsync(string bucket, string objectName, string contentType, IFormFile file)
        {
            if (file == null || file.Length <= 0)
                return (false, "Archivo vacío.");

            try
            {
                var minio = _fileManager.GetMinio();

                // VERIFICAR SI EL BUCKET EXISTE PRIMERO
                bool bucketExists = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
                if (!bucketExists)
                {
                    // Intentar crear el bucket si no existe
                    try
                    {
                        await minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
                        _logger.LogInformation($"Bucket '{bucket}' creado exitosamente");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error al crear bucket '{bucket}'");
                        return (false, $"Error al crear bucket '{bucket}': {ex.Message}");
                    }
                }

                await using var fileStream = new MemoryStream();
                await file.CopyToAsync(fileStream);
                fileStream.Position = 0;

                Stream finalStream = fileStream;

                // MANEJO DE IMÁGENES CON MEJOR CONTROL DE ERRORES
                if (contentType.StartsWith("image/"))
                {
                    try
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
                        else
                        {
                            fileStream.Position = 0; // Reset stream si no se redimensiona
                        }
                    }
                    catch (UnknownImageFormatException ex)
                    {
                        _logger.LogWarning(ex, "Formato de imagen no reconocido. Subiendo archivo original.");
                        fileStream.Position = 0;
                        finalStream = fileStream;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al procesar imagen");
                        fileStream.Position = 0;
                        finalStream = fileStream;
                    }
                }

                // SUBIR EL ARCHIVO (REEMPLAZA SI EXISTE)
                await ProcessAndStoreObject(bucket, objectName, contentType, finalStream);

                var tipo = contentType.StartsWith("image") ? "Imagen" :
                           contentType.StartsWith("audio") ? "Audio" :
                           contentType.StartsWith("application/pdf") ? "PDF" :
                           "Archivo";

                return (true, $"¡{tipo} subido exitosamente!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UploadObjectAsync");
                return (false, $"Error al subir objeto: {ex.Message}");
            }
        }

        // Obtener cualquier tipo de objeto desde Minio
        public async Task<(bool Success, byte[] Data, string ContentType, string Message)> GetObjectAsync(string bucket, string objectName)
        {
            try
            {
                var minio = _fileManager.GetMinio();

                // Verificar si el bucket existe
                bool bucketExists = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
                if (!bucketExists)
                {
                    return (false, null, null, $"El bucket '{bucket}' no existe.");
                }

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
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                return (false, null, null, $"El objeto '{objectName}' no existe en el bucket '{bucket}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetObjectAsync");
                return (false, null, null, $"Error al obtener el objeto: {ex.Message}");
            }
        }

        // Eliminar un objeto específico de un bucket
        public async Task<(bool Success, string Message)> DeleteObjectAsync(string bucket, string objectName)
        {
            try
            {
                var minio = _fileManager.GetMinio();

                // Verificar si el bucket existe
                bool bucketExists = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
                if (!bucketExists)
                {
                    return (false, $"El bucket '{bucket}' no existe.");
                }

                var rmArgs = new RemoveObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectName);

                await minio.RemoveObjectAsync(rmArgs);

                return (true, $"Objeto '{objectName}' eliminado correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en DeleteObjectAsync");
                return (false, $"Error al eliminar el objeto '{objectName}': {ex.Message}");
            }
        }

        // MÉTODO PRIVADO PARA ALMACENAR EL OBJETO EN MINIO (REEMPLAZA SI EXISTE)
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
                .WithContentType(contentType);

            await minio.PutObjectAsync(args);

            _logger.LogInformation($"Archivo '{objectName}' almacenado en bucket '{bucket}'");
        }
    }
}