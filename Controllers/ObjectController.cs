using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using web_api_users.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;

namespace web_api_users.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [SwaggerTag("Gestión de archivos en MinIO (subida, descarga y eliminación).")]
    public class ObjectController : ControllerBase
    {
        private readonly IObjectService _objectService;
        private readonly ILogger<ObjectController> _logger;

        public ObjectController(IObjectService objectService, ILogger<ObjectController> logger)
        {
            _objectService = objectService;
            _logger = logger;
        }

        /// <summary>
        /// Sube un archivo al bucket especificado. Reemplaza si ya existe.
        /// </summary>
        [Authorize(Policy = "MinioObjectUpload")]
        [HttpPost("UploadObject")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(
            Summary = "Subir archivo",
            Description = "Sube un archivo al bucket de MinIO. Reemplaza el archivo si ya existe."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Archivo subido/reemplazado exitosamente.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Archivo no válido.")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Error interno del servidor.")]
        public async Task<IActionResult> UploadObject(
            [FromQuery, SwaggerParameter("Nombre del bucket. Ej: 'product-images'", Required = true)] string nameBucket,
            [FromQuery, SwaggerParameter("Nombre del archivo al guardar. Ej: 'product_37'", Required = true)] string nameObject,
            [FromQuery, SwaggerParameter("Tipo MIME del archivo. Ej: 'image/jpeg'", Required = true)] string contentType,
            [FromForm, SwaggerParameter("Archivo a subir", Required = true)] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Archivo no válido" });
            }

            try
            {
                _logger.LogInformation($"Subiendo archivo: {nameObject} al bucket: {nameBucket}, tipo: {contentType}, tamaño: {file.Length} bytes");

                var result = await _objectService.UploadObjectAsync(nameBucket, nameObject, contentType, file);

                if (!result.Success)
                {
                    _logger.LogWarning($"Error al subir archivo: {result.Message}");
                    return StatusCode(500, new { success = false, message = result.Message });
                }

                _logger.LogInformation("Archivo subido exitosamente");

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    objectName = nameObject,
                    bucket = nameBucket
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UploadObject");
                return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
            }
        }

        /// <summary>
        /// Descarga un archivo desde MinIO.
        /// </summary>
        [Authorize(Policy = "MinioObjectDownload")]
        [HttpGet("GetObjectMINio")]
        [SwaggerOperation(
            Summary = "Descargar archivo",
            Description = "Obtiene un archivo almacenado en MinIO, indicando el bucket y el nombre del archivo."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Archivo descargado correctamente.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Archivo no encontrado.")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Error al obtener el archivo.")]
        public async Task<IActionResult> GetObjectMINio(
            [FromQuery, SwaggerParameter("Nombre del bucket. Ej: 'product-images'", Required = true)] string nameBucket,
            [FromQuery, SwaggerParameter("Nombre del archivo a descargar. Ej: 'product_37'", Required = true)] string nameObject)
        {
            try
            {
                _logger.LogInformation($"Descargando archivo: {nameObject} del bucket: {nameBucket}");

                var result = await _objectService.GetObjectAsync(nameBucket, nameObject);

                if (!result.Success)
                {
                    _logger.LogWarning($"Error al descargar archivo: {result.Message}");
                    return NotFound(new { success = false, message = result.Message });
                }

                _logger.LogInformation("Archivo descargado exitosamente");
                return File(result.Data, result.ContentType, nameObject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetObjectMINio");
                return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
            }
        }

        /// <summary>
        /// Elimina un archivo de MinIO.
        /// </summary>
        [Authorize(Policy = "MinioObjectDelete")]
        [HttpDelete("DeleteObjectMINio")]
        [SwaggerOperation(
            Summary = "Eliminar archivo",
            Description = "Elimina un archivo desde un bucket especificado en MinIO."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Archivo eliminado correctamente.")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Error al eliminar el archivo.")]
        public async Task<IActionResult> DeleteObjectMINio(
            [FromQuery, SwaggerParameter("Nombre del bucket. Ej: 'product-images'", Required = true)] string bucket,
            [FromQuery, SwaggerParameter("Nombre del archivo a eliminar. Ej: 'product_37'", Required = true)] string objectname)
        {
            try
            {
                _logger.LogInformation($"Eliminando archivo: {objectname} del bucket: {bucket}");

                var result = await _objectService.DeleteObjectAsync(bucket, objectname);

                if (!result.Success)
                {
                    _logger.LogWarning($"Error al eliminar archivo: {result.Message}");
                    return StatusCode(500, new { success = false, message = result.Message });
                }

                _logger.LogInformation("Archivo eliminado exitosamente");
                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en DeleteObjectMINio");
                return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
            }
        }
    }
}