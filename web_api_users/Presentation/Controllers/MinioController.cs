using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Minio;
using System;
using System.Collections.Generic;

using System.IO;
using System.Threading.Tasks;
using web_api_users.Domain.Interfaces;
using web_api_users.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace web_api_users.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MinioController : ControllerBase
    {
        private readonly IFileManager _fileManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IMinioService _minioService;


        public MinioController(IFileManager fileManager, IWebHostEnvironment environment, IMinioService minioService)
        {
            _fileManager = fileManager;
            _environment = environment;
            _minioService = minioService;
        }

        [HttpPost]
        [Route("CreateBucketMINio")]
        public async Task<IActionResult> CreateBucketMINio(string name)
        {
            var result = await _minioService.CreateBucket(name);

            if (!result.Success)
                return Conflict(result.Message);

            return Ok(result.Message);
        }

        [HttpGet]
        [Route("ListBucketsMINio")]
        public async Task<IActionResult> ListBucketsMINio()
        {
            var buckets = await _minioService.ListBuckets();
            return Ok(buckets);
        }

        [HttpGet]
        [Route("ListObjectsMINio")]
        public async Task<IActionResult> ListObjectsMINio(string name)
        {
            var result = await _minioService.ListObjects(name);
            if (!result.Success)
                return Conflict(result.Message);
            return Ok(result.Objects);
        }

        [HttpDelete]
        [Route("DeleteBucketMINio")]
        public async Task<IActionResult> DeleteBucketMINio(string name)
        {
            var result = await _minioService.DeleteBucket(name);
            if (!result.Success)
                return Conflict(result.Message);
            return Ok(result.Message);
        }

        [HttpPost]
        [Route("CreateObjectMINio")]
        public async Task<IActionResult> CreateObjectMINio(string nameBucket, string nameObject, string contentType, IFormFile file)
        {
            if (file.Length <= 0)
                return BadRequest("Empty file");

            try
            {
                StatObjectArgs statObjectArgs = new StatObjectArgs().
                                                    WithBucket(nameBucket).
                                                    WithObject(nameObject);

                var obj = await _fileManager.GetMinio().StatObjectAsync(statObjectArgs);

                return Conflict($"Se encontro el object: {nameObject} existente, no se puede reemplazar, cambie el nombre!");
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                List<string> allowedContentTypes = new()
                {
                "image/apng",
                "image/avif",
                "image/jpeg",
                "image/png",
                "image/svg+xml",
                "image/webp"
                };

                // Si el archivo es un tipo de imagen permitido
                if (allowedContentTypes.Contains(contentType))
                {
                    int maxResolutionWidth = 1200;
                    int maxResolutionHeight = 600;

                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;

                        using (var image = Image.Load(memoryStream))
                        {
                            if (image.Width > maxResolutionWidth && image.Height > maxResolutionHeight)
                            {
                                image.Mutate(x => x.Resize(new ResizeOptions
                                {
                                    Size = new Size(maxResolutionWidth, maxResolutionHeight),
                                    Mode = ResizeMode.Max // Otras opciones son disponibles
                                }));

                                using var resizedStream = new MemoryStream();
                                image.Save(resizedStream, new JpegEncoder()); // Cambia el codificador según tus necesidades
                                resizedStream.Position = 0;
                                // Continuar con el procesamiento y almacenamiento del objeto reducido
                                await ProcessAndStoreImage(nameBucket, nameObject, contentType, resizedStream);
                                return Ok("¡Se creó la imagen!");
                            }
                            else
                            {
                                // Continuar con el procesamiento y almacenamiento del objeto original
                                using var stream = file.OpenReadStream();
                                await ProcessAndStoreImage(nameBucket, nameObject, contentType, stream);
                                return Ok("¡Se creó la imagen!");
                            }
                        }
                    }
                }
                else
                {
                    // Continuar con el procesamiento y almacenamiento del objeto original
                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;

                        await ProcessAndStoreImage(nameBucket, nameObject, contentType, memoryStream);
                        return Ok("¡Se creó la imagen!");
                    }
                }
            }
            catch (Exception ex)
            {
                return Conflict($"No se creo el object: {nameObject} - el error es:" + ex);
            }
        }

        [HttpPost]
        [Route("CreateObjectMp3MINio")]
        public async Task<IActionResult> CreateObjectMp3MINio(string nameBucket, string nameObject, string contentType, IFormFile file)
        {
            if (file.Length <= 0)
                return BadRequest("Empty file");

            try
            {
                StatObjectArgs statObjectArgs = new StatObjectArgs().
                                                    WithBucket(nameBucket).
                                                    WithObject(nameObject);

                var obj = await _fileManager.GetMinio().StatObjectAsync(statObjectArgs);

                return Conflict($"Se encontro el object: {nameObject} existente, no se puede reemplazar, cambie el nombre!");
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                
                // Continuar con el procesamiento y almacenamiento del objeto original
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    PutObjectArgs args = new PutObjectArgs()
                    .WithBucket(nameBucket)
                    .WithObject(nameObject)
                    .WithStreamData(memoryStream)
                    .WithObjectSize(memoryStream.Length)
                    .WithContentType(contentType)
                    .WithServerSideEncryption(null);
                    await _fileManager.GetMinio().PutObjectAsync(args);
                    return Ok("¡Se creó el audio!");
                }
            }
            catch (Exception ex)
            {
                return Conflict($"No se creo el object: {nameObject} - el error es:" + ex);
            }
        }

        [HttpGet]
        [Route("GetObjectMp3MINio")]
        public async Task<IActionResult> GetObjectMp3MINio(string nameBucket, string nameObject)
        {
        byte[] bytesFromMp3 = null;
        try
            {
                StatObjectArgs statObjectArgs = new StatObjectArgs().WithBucket(nameBucket).WithObject(nameObject);
                await _fileManager.GetMinio().StatObjectAsync(statObjectArgs);

                GetObjectArgs args = new GetObjectArgs().WithCallbackStream((stream) =>
                {
                    byte[] buffer = new byte[16 * 1024];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                        }
                        bytesFromMp3 = ms.ToArray();
                    }
                })
                .WithBucket(nameBucket)
                .WithObject(nameObject);

                await _fileManager.GetMinio().GetObjectAsync(args);

                var contentType = "audio/mpeg";
                return File(fileContents: bytesFromMp3, contentType);
            }
            catch (Exception ex)
            {
                return Conflict($"No se encontro el object: {nameObject}" + ex);
            }
        }      

        private async Task ProcessAndStoreImage(string nameBucket, string nameObject, string contentType, Stream stream)
        {
            // Puedes realizar cualquier procesamiento adicional necesario antes de almacenar la imagen
            // ...

            PutObjectArgs args = new PutObjectArgs()
                .WithBucket(nameBucket)
                .WithObject(nameObject)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType)
                .WithServerSideEncryption(null);

            await _fileManager.GetMinio().PutObjectAsync(args);
        }

        [HttpGet]
        [Route("GetObjectMINio")]
        public async Task<IActionResult> GetObjectMINio(string nameBucket, string nameObject)
        {
            byte[] bytesFromPhoto = null;
            try
            {
                StatObjectArgs statObjectArgs = new StatObjectArgs().WithBucket(nameBucket).WithObject(nameObject);

                await _fileManager.GetMinio().StatObjectAsync(statObjectArgs);

                GetObjectArgs args = new GetObjectArgs().WithCallbackStream((stream) =>
                {
                    byte[] buffer = new byte[16 * 1024];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                        }
                        bytesFromPhoto = ms.ToArray();
                    }
                })
                    .WithBucket(nameBucket)
                    .WithObject(nameObject);

                var objeto = await _fileManager.GetMinio().GetObjectAsync(args);
                var typodoc = objeto.ContentType;
                byte[] bphoto = bytesFromPhoto;
                return File(fileContents: bphoto, typodoc);
            }
            catch (Exception ex)
            {
                return Conflict($"No se encontro el object: {nameObject}" + ex);
            }
        }

        [HttpDelete]
        [Route("DeleteObjectMINio")]
        public async Task<IActionResult> DeleteObjectMINio(string bucket, string objectname)
        {
            try
            {
                StatObjectArgs statObjectArgs = new StatObjectArgs().WithBucket(bucket).WithObject(objectname);

                await _fileManager.GetMinio().StatObjectAsync(statObjectArgs);

                RemoveObjectArgs rmArgs = new RemoveObjectArgs()
                                              .WithBucket(bucket)
                                              .WithObject(objectname);
                await _fileManager.GetMinio().RemoveObjectAsync(rmArgs);

                return Ok($"se borro el object {objectname}");
            }
            catch (Exception ex)
            {
                return Conflict($"No se borro el object: {objectname}: -" + ex);
            }
        }
    }
}
