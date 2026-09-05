# Pruebas y cobertura

Requisito: SDK .NET 10 (el proyecto de pruebas usa `net10.0`; la API mantiene `net6.0`).
Las pruebas usan xUnit, Moq y el SDK real de MinIO con un transporte HTTP en memoria.
No necesitan MinIO, WSO2, credenciales reales ni puertos abiertos.

Desde la raíz del repositorio:

```powershell
dotnet test tests/web_api_users.Tests/web_api_users.Tests.csproj -p:CollectCoverage=true
```

El comando genera `tests/web_api_users.Tests/TestResults/Coverage/coverage.opencover.xml`
y falla si la cobertura total de líneas **o** de ramas baja del 85%.
Los valores predeterminados se definen en el proyecto de pruebas; ejecutar solamente
`dotnet test` no activa la medición ni el umbral.

## Alcance

- Controladores: respuestas correctas, validación de archivos, errores y excepciones.
- Buckets: creación, existencia, listado y mapeo de metadatos, eliminación y errores del SDK.
- Objetos: bytes y MIME de la subida, creación automática de buckets, imágenes dentro y
  fuera del límite, formato no reconocido, descarga, objetos inexistentes y eliminación.
- Arranque: registro de dependencias, JWT, las siete políticas de permisos, CORS,
  Swagger y registro de rutas protegidas en Development y Production.

Única exclusión de cobertura: `Properties/Resources.Designer.cs`, generado por herramientas.
No se excluyen servicios, controladores, DTOs, `Startup.cs` ni `Program.cs`.

La versión actual de MinIO devuelve un error para el listado de un bucket vacío;
la prueba correspondiente verifica cómo el servicio comunica ese error, sin cambiar
el comportamiento de la API.

## SonarQube y Jenkins

`devops/JenkinsfileDev.groovy` ejecuta `begin`, compilación, pruebas con cobertura y `end`.
El parámetro de importación y la salida de Coverlet apuntan al mismo archivo:

```text
/d:sonar.cs.opencover.reportsPaths="tests/web_api_users.Tests/TestResults/Coverage/coverage.opencover.xml"
/d:sonar.coverage.exclusions="**/Properties/Resources.Designer.cs"
```

Antes, Jenkins buscaba en `TestResults/**/coverage.opencover.xml` desde la raíz,
pero Coverlet escribía dentro del proyecto de pruebas. Ahora también se comprueba
que el informe exista y no esté vacío antes de cerrar el análisis. El archivo
`sonar-project.properties` se restaura incluso si falla una prueba o el scanner.

SonarScanner for .NET recibe estos parámetros en `begin`; no lee
`sonar-project.properties` como configuración. Este último conserva la ruta equivalente
para referencia. Véase la [documentación oficial de cobertura .NET](https://docs.sonarsource.com/sonarqube-community-build/analyzing-source-code/test-coverage/dotnet-test-coverage).

`devops/JenkinsfileProd.groovy` también exige las pruebas y el umbral antes del despliegue;
ese pipeline no tenía una etapa de SonarQube y no publica análisis.

Para actualizar el panel de SonarQube hay que incorporar estos cambios y ejecutar el
pipeline de desarrollo. La medición local no actualiza el servidor por sí sola.
En el log del scanner, comprobar la importación del archivo OpenCover indicado arriba.
