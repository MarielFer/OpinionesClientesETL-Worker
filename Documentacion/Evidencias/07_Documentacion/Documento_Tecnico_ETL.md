# DOCUMENTACION TECNICA - PROYECTO ETL

## 1. Objetivo
Desarrollar un servicio Worker en .NET 8 para extraer datos desde archivos CSV, una base de datos relacional y una API REST, y almacenarlos temporalmente en archivos JSON (Staging).

## 2. Arquitectura
La solucion utiliza una arquitectura modular por capas: Domain define los contratos y modelos; Application organiza los casos de uso; Infrastructure implementa los extractores y el almacenamiento; Worker coordina la ejecucion mediante inyeccion de dependencias.

## 3. Fuentes de datos
- CSV: archivo interno surveys_part1.csv, con 500 registros de encuestas.
- SQL Server: base OpinionesClientesDB, con 6 registros extraidos mediante consulta.
- API REST: JSONPlaceholder, endpoint /comments, con 500 registros de prueba. Esta API es publica y sus datos son demostrativos, no opiniones reales de clientes.

## 4. Proceso de extraccion
El Worker lee la configuracion, identifica la fuente seleccionada, ejecuta el extractor correspondiente, procesa los registros y los entrega al componente de Staging. Los resultados se almacenan en archivos JSON. La ejecucion utiliza operaciones asincronas y registro de eventos.

## 5. Tecnologias utilizadas
- .NET 8 Worker Service y C#.
- CsvHelper para lectura de archivos CSV.
- Microsoft.Data.SqlClient para acceso a SQL Server.
- HttpClient para consumo de API REST.
- Inyeccion de dependencias para desacoplar componentes.
- ILogger y Serilog para registro de eventos y errores.
- JSON para almacenamiento temporal en Staging.

## 6. Seguridad
La cadena de conexion de SQL Server se almacena mediante .NET User Secrets y no directamente en appsettings.json. El archivo .gitignore excluye archivos de configuracion sensibles, registros, Staging y carpetas de compilacion.

## 7. Resultados de las pruebas
Se verifico la extraccion de 500 registros desde CSV, 6 desde SQL Server y 500 desde la API REST. En conjunto, las pruebas de las tres fuentes produjeron 1,006 registros almacenados en Staging. Se verificaron los archivos JSON y los registros de ejecucion. Las ejecuciones de prueba reportaron cero errores en los resultados documentados.

## 8. Manejo de errores y monitoreo
El servicio registra eventos mediante ILogger y Serilog, contempla el manejo de excepciones y genera un resumen con fuente, registros extraidos, registros guardados, errores y duracion. Los logs se almacenan en la carpeta Logs.

## 9. Decisiones de arquitectura
Se eligio Worker Service porque permite ejecutar procesos de fondo. La separacion por capas facilita mantenimiento y ampliacion. Los extractores independientes permiten incorporar nuevas fuentes. JSON se utiliza como almacenamiento temporal para desacoplar la extraccion de las etapas posteriores.

## 10. Alcance y trabajo futuro
Esta actividad implementa la etapa de extraccion y almacenamiento temporal. La carga a un Data Warehouse, las transformaciones analiticas avanzadas y la construccion de dashboards corresponden a etapas posteriores y no se presentan como funcionalidades completadas.

## 11. Diagramas
Se incluyen los archivos Diagrama_Arquitectura_ETL.mmd y Diagrama_Flujo_Extraccion_ETL.mmd, con la arquitectura general y el flujo de extraccion.

## 12. Conclusion
Se implemento y verifico un servicio de extraccion modular en .NET 8 capaz de obtener datos desde tres tipos de fuentes y almacenarlos en archivos JSON, con registro de ejecucion, manejo de errores y proteccion de credenciales.
