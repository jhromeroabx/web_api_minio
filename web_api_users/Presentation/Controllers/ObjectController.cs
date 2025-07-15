using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using web_api_users.Domain.Interfaces;

namespace web_api_users.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [SwaggerTag("Gestión de archivos en MinIO (subida, descarga y eliminación).")]
    public class ObjectController : ControllerBase
    {
        private readonly IObjectService _objectService;

        public ObjectController(IObjectService objectService)
        {
            _objectService = objectService;
        }

        /// <summary>
        /// Sube un archivo al bucket especificado.
        /// </summary>
        [HttpPost("UploadObject")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(
    Summary = "Subir archivo",
    Description = "Sube un archivo al bucket de MinIO. Solo se aceptan archivos tipo imagen (JPG/PNG), audio (MP3) o PDF."
)]
        [SwaggerResponse(StatusCodes.Status200OK, "Archivo subido exitosamente.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "El archivo ya existe o hubo un error.")]
        public async Task<IActionResult> UploadObject(
    [FromQuery, SwaggerParameter("Nombre del bucket. Ej: 'documentos-usuarios'", Required = true)] string nameBucket = "documentos-usuarios",
    [FromQuery, SwaggerParameter("Nombre del archivo al guardar. Ej: 'dni_frontal'", Required = true)] string nameObject = "dni_frontal",
    [FromQuery, SwaggerParameter("Tipo MIME del archivo. Acepta:\n- image/jpeg (JPG)\n- image/png (PNG)\n- application/pdf (PDF)\n- audio/mpeg (MP3)", Required = true)] string contentType = "application/pdf",
    [SwaggerParameter("Archivo a subir (JPG, PNG, MP3 o PDF).", Required = true)] IFormFile file=null)
        {
            var result = await _objectService.UploadObjectAsync(nameBucket, nameObject, contentType, file);

            if (!result.Success)
                return Conflict(result.Message);

            return Ok(result.Message);
        }


        /// <summary>
        /// Descarga un archivo desde MinIO.
        /// </summary>
        [HttpGet("GetObjectMINio")]
        [SwaggerOperation(
            Summary = "Descargar archivo",
            Description = "Obtiene un archivo almacenado en MinIO, indicando el bucket y el nombre del archivo."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Archivo descargado correctamente.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Error al obtener el archivo.")]
        public async Task<IActionResult> GetObjectMINio(
            [FromQuery, SwaggerParameter("Nombre del bucket. Ej: 'documentos-usuarios'", Required = true)] string nameBucket = "documentos-usuarios",
            [FromQuery, SwaggerParameter("Nombre del archivo a descargar. Ej: 'dni_frontal'", Required = true)] string nameObject = "dni_frontal")
        {
            var result = await _objectService.GetObjectAsync(nameBucket, nameObject);

            if (!result.Success)
                return Conflict(result.Message);

            return File(result.Data, result.ContentType);
        }

        /// <summary>
        /// Elimina un archivo de MinIO.
        /// </summary>
        [HttpDelete("DeleteObjectMINio")]
        [SwaggerOperation(
            Summary = "Eliminar archivo",
            Description = "Elimina un archivo desde un bucket especificado en MinIO."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Archivo eliminado correctamente.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Error al eliminar el archivo.")]
        public async Task<IActionResult> DeleteObjectMINio(
            [FromQuery, SwaggerParameter("Nombre del bucket. Ej: 'documentos-usuarios'", Required = true)] string bucket = "documentos-usuarios",
            [FromQuery, SwaggerParameter("Nombre del archivo a eliminar. Ej: 'dni_frontal'", Required = true)] string objectname = "dni_frontal")
        {
            var result = await _objectService.DeleteObjectAsync(bucket, objectname);

            if (!result.Success)
                return Conflict(result.Message);

            return Ok(result.Message);
        }
    }
}
