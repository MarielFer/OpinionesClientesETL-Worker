/*
 PROYECTO: Sistema de Analisis de Opiniones de Clientes
 ACTIVIDAD: Carga de dimensiones del Data Warehouse
 ESTUDIANTE: Mariel Fernandez
 MATRICULA: 2022-1314
 ASIGNATURA: Electiva 1 - Big Data
 FECHA: 25 de noviembre de 2026

 ORIGEN: OpinionesClientesDB
 DESTINO: OpinionesClientesDW

 Carga incremental. No elimina registros existentes.
*/

USE OpinionesClientesDW;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LANGUAGE Spanish;
SET DATEFIRST 1;

BEGIN TRY
    BEGIN TRANSACTION;

    /* 1. DIMENSION CANAL */
    INSERT INTO dbo.DimCanal (IdCanal, Canal, Plataforma)
    SELECT V.IdCanal, V.Canal, V.Plataforma
    FROM (VALUES
        (1, N'Encuesta interna', N'No aplica'),
        (2, N'Rese' + NCHAR(241) + N'a web', N'No aplica'),
        (3, N'Red social', N'Instagram'),
        (4, N'Red social', N'Facebook'),
        (5, N'Red social', N'Twitter')
    ) AS V(IdCanal, Canal, Plataforma)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.DimCanal D
        WHERE D.Canal = V.Canal
          AND D.Plataforma = V.Plataforma
    );

    /* 2. DIMENSION CLASIFICACION */
    INSERT INTO dbo.DimClasificacion
        (IdClasificacion, Nombre, ValorSentimiento)
    SELECT C.IdClasificacion, C.Nombre,
        CASE C.Nombre
            WHEN N'Positiva' THEN 1
            WHEN N'Negativa' THEN -1
            WHEN N'Neutra' THEN 0
        END
    FROM OpinionesClientesDB.dbo.Clasificaciones C
    WHERE C.Nombre IN (N'Positiva', N'Negativa', N'Neutra')
      AND NOT EXISTS (
          SELECT 1 FROM dbo.DimClasificacion D
          WHERE D.Nombre = C.Nombre
      );

    /* 3. CLIENTE NO IDENTIFICADO */
    IF NOT EXISTS (
        SELECT 1 FROM dbo.DimCliente WHERE IdClienteDW = 0
    )
    BEGIN
        SET IDENTITY_INSERT dbo.DimCliente ON;

        INSERT INTO dbo.DimCliente
            (IdClienteDW, IdClienteOrigen, Nombre, Email,
             TipoCliente, Pais, RangoEdad)
        VALUES
            (0, NULL, N'Cliente no identificado', NULL,
             N'No identificado', N'No disponible', N'No disponible');

        SET IDENTITY_INSERT dbo.DimCliente OFF;
    END;

    /* 4. CLIENTES IDENTIFICADOS */
    INSERT INTO dbo.DimCliente
        (IdClienteOrigen, Nombre, Email, TipoCliente, Pais, RangoEdad)
    SELECT C.IdCliente, C.Nombre, C.Email,
           N'Identificado', N'No especificado', N'No especificado'
    FROM OpinionesClientesDB.dbo.Clientes C
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.DimCliente D
        WHERE D.IdClienteOrigen = C.IdCliente
    );

    /* 5. DIMENSION PRODUCTO */
    INSERT INTO dbo.DimProducto
        (IdProductoOrigen, Nombre, Categoria)
    SELECT P.IdProducto, P.Nombre, C.Nombre
    FROM OpinionesClientesDB.dbo.Productos P
    INNER JOIN OpinionesClientesDB.dbo.Categorias C
        ON P.IdCategoria = C.IdCategoria
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.DimProducto D
        WHERE D.IdProductoOrigen = P.IdProducto
    );

    /* 6. DIMENSION FECHA: 2024 A 2026 */
    ;WITH Calendario AS (
        SELECT DATEFROMPARTS(2024, 1, 1) AS Fecha
        UNION ALL
        SELECT DATEADD(DAY, 1, Fecha)
        FROM Calendario
        WHERE Fecha < DATEFROMPARTS(2026, 12, 31)
    )
    INSERT INTO dbo.DimFecha
        (IdFecha, Fecha, Anio, Trimestre, AnioTrimestre,
         Mes, NombreMes, AnioMes, Dia, DiaSemana,
         NombreDia, EsFinDeSemana)
    SELECT
        CONVERT(INT, CONVERT(CHAR(8), Fecha, 112)),
        Fecha,
        YEAR(Fecha),
        DATEPART(QUARTER, Fecha),
        CONCAT(YEAR(Fecha), N'-T', DATEPART(QUARTER, Fecha)),
        MONTH(Fecha),
        UPPER(LEFT(DATENAME(MONTH, Fecha), 1))
            + SUBSTRING(DATENAME(MONTH, Fecha), 2, 14),
        CONVERT(CHAR(7), Fecha, 120),
        DAY(Fecha),
        DATEPART(WEEKDAY, Fecha),
        UPPER(LEFT(DATENAME(WEEKDAY, Fecha), 1))
            + SUBSTRING(DATENAME(WEEKDAY, Fecha), 2, 14),
        CASE WHEN DATEPART(WEEKDAY, Fecha) IN (6, 7)
             THEN 1 ELSE 0 END
    FROM Calendario C
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.DimFecha D
        WHERE D.Fecha = C.Fecha
    )
    OPTION (MAXRECURSION 0);

    /* 7. DIMENSION PALABRA CLAVE */
    INSERT INTO dbo.DimPalabraClave (Palabra, Polaridad)
    SELECT V.Palabra, V.Polaridad
    FROM (VALUES
        (N'excelente', N'Positiva'),
        (N'encanta', N'Positiva'),
        (N'recomendable', N'Positiva'),
        (N'recomiendo', N'Positiva'),
        (N'superior', N'Positiva'),
        (N'contento', N'Positiva'),
        (N'perfecto', N'Positiva'),
        (N'satisfecho', N'Positiva'),
        (N'gran relacion', N'Positiva'),
        (N'mala', N'Negativa'),
        (N'pesima', N'Negativa'),
        (N'danado', N'Negativa'),
        (N'tardio', N'Negativa'),
        (N'rompio', N'Negativa'),
        (N'decepcionado', N'Negativa'),
        (N'insatisfecho', N'Negativa'),
        (N'no cumple', N'Negativa'),
        (N'no funciona', N'Negativa'),
        (N'no volveria', N'Negativa')
    ) AS V(Palabra, Polaridad)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.DimPalabraClave D
        WHERE D.Palabra = V.Palabra
    );

    /*
      8. DIMENSION EVENTO
      Sin registros: no existe una tabla de eventos o campanas
      en las fuentes verificadas. No se inventan registros.
    */

    COMMIT TRANSACTION;
    PRINT N'Carga de dimensiones completada correctamente.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* 9. RESUMEN DE DIMENSIONES */
SELECT 'DimCanal' AS Tabla, COUNT(*) AS Registros
FROM dbo.DimCanal
UNION ALL
SELECT 'DimClasificacion', COUNT(*) FROM dbo.DimClasificacion
UNION ALL
SELECT 'DimCliente', COUNT(*) FROM dbo.DimCliente
UNION ALL
SELECT 'DimEvento', COUNT(*) FROM dbo.DimEvento
UNION ALL
SELECT 'DimFecha', COUNT(*) FROM dbo.DimFecha
UNION ALL
SELECT 'DimPalabraClave', COUNT(*) FROM dbo.DimPalabraClave
UNION ALL
SELECT 'DimProducto', COUNT(*) FROM dbo.DimProducto;
GO