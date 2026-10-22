-- MIGRACIONES para una base ya existente.
-- Ejecute todos los bloques "MIGRACIÓN" en orden; se pueden repetir sin dañar los datos.
-- NO ejecute la sección "DATOS DE PRUEBA" del final en la base real.

-- MIGRACIÓN RBAC: tabla de permisos por rol (el Administrador no necesita filas).
USE GeovannysBarberDB;
GO
IF OBJECT_ID(N'dbo.RolModulo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolModulo (
        rol VARCHAR(30) NOT NULL,
        modulo NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_RolModulo PRIMARY KEY (rol, modulo)
    );
END;
GO
INSERT INTO dbo.RolModulo (rol, modulo)
SELECT p.rol, p.modulo
FROM (VALUES
    ('Trabajador', N'Mi jornada'),
    ('Cajera', N'Clientes'), ('Cajera', N'Facturación'),
    ('Cajero', N'Clientes'), ('Cajero', N'Facturación')
) AS p(rol, modulo)
WHERE NOT EXISTS (SELECT 1 FROM dbo.RolModulo r WHERE r.rol = p.rol AND r.modulo = p.modulo);
GO
-- FIN MIGRACIÓN RBAC.

-- MIGRACIÓN SEGURIDAD DEL LOGIN para una base ya existente.
-- Agrega el control de intentos fallidos y reemplaza las claves de prueba en texto plano por su hash BCrypt.
-- Cada usuario conserva la misma contraseña; solo cambia la forma en que se guarda.
USE GeovannysBarberDB;
GO
IF COL_LENGTH(N'dbo.Usuario', N'intentos_fallidos') IS NULL
    ALTER TABLE dbo.Usuario ADD intentos_fallidos INT NOT NULL CONSTRAINT DF_Usuario_intentos_fallidos DEFAULT 0;
IF COL_LENGTH(N'dbo.Usuario', N'bloqueado_hasta') IS NULL
    ALTER TABLE dbo.Usuario ADD bloqueado_hasta DATETIME2 NULL;
GO
UPDATE u SET contrasena_hash = h.hash
FROM dbo.Usuario u
JOIN (VALUES
    ('DonGio', 'Gato1979*#', '$2a$11$gNfzhyeyz8iz1UopwqlR4OGGCfG50QRTVAqNG/MM0zuvzcI5aKVsC'),
    ('DonNoe', 'hash_noe123', '$2a$11$.LXWLQR/JYY5dFGiJl1l8u6F/onzzqRZUuVPbUcvndySBv3K8GBkS'),
    ('DonNoe', 'NuevaClaveNoe2026*', '$2a$11$XtYlX0hXomMVHBE2u.FbPuoEYs55KZLebm7o5LZTBt9XIRvoBIhdG'),
    ('Lis', 'hash_lis123', '$2a$11$ZQARC8AodD.3jaOd39FMF.1hkN9lHy15rAEj.AH9PopSfeM8QfSkq'),
    ('Alex', 'hash_alex123', '$2a$11$FzBcXB89jUUDkJuHdSF9JesJRgrCJWKXMNQ1DTHyHH6FBpUzhglwG'),
    ('Willy', 'hash_willi123', '$2a$11$0xSRU8yGwoBrVToadr8IqObMF30DiWf0ceCxAwxVuRwL5M6yjMTgq')
) AS h(usuario, clave, hash) ON u.nombre_usuario = h.usuario AND u.contrasena_hash = h.clave;
-- Cuentas que siguen sin hash: no podrán entrar hasta que el administrador use "Restablecer clave".
SELECT id_usuario, nombre_usuario FROM dbo.Usuario WHERE contrasena_hash IS NULL OR contrasena_hash NOT LIKE '$2%';
GO
-- FIN MIGRACIÓN SEGURIDAD DEL LOGIN.

-- MIGRACIÓN MÓDULO TRABAJADORES para una base ya existente.
-- Agrega el salario, la fecha en que se resolvió cada préstamo y el registro de descuentos al salario.
USE GeovannysBarberDB;
GO
IF COL_LENGTH(N'dbo.Trabajador', N'salario') IS NULL
    ALTER TABLE dbo.Trabajador ADD salario DECIMAL(10,2) NULL;
IF COL_LENGTH(N'dbo.Prestamo', N'fecha_resolucion') IS NULL
    ALTER TABLE dbo.Prestamo ADD fecha_resolucion DATETIME2 NULL;
IF OBJECT_ID(N'dbo.Descuento_Prestamo', N'U') IS NULL
    CREATE TABLE dbo.Descuento_Prestamo (
        id_descuento INT IDENTITY(1,1) PRIMARY KEY,
        id_prestamo INT NOT NULL,
        fecha DATETIME2 NOT NULL,
        monto DECIMAL(10,2) NOT NULL CHECK (monto > 0),
        observacion VARCHAR(100) NULL,
        id_usuario INT NULL,
        FOREIGN KEY (id_prestamo) REFERENCES dbo.Prestamo(id_prestamo),
        FOREIGN KEY (id_usuario) REFERENCES dbo.Usuario(id_usuario)
    );
GO
-- Las solicitudes pendientes guardadas con saldo 0 por el error anterior recuperan su monto.
UPDATE dbo.Prestamo SET saldo_pendiente = monto WHERE estado = 'Pendiente' AND saldo_pendiente = 0;
GO
-- FIN MIGRACIÓN MÓDULO TRABAJADORES.

-- MIGRACIÓN MÓDULO CLIENTES para una base ya existente.
-- Agrega el NIT; los clientes existentes quedan como consumidor final (CF) hasta que se edite su NIT.
USE GeovannysBarberDB;
GO
IF COL_LENGTH(N'dbo.Cliente', N'nit') IS NULL
    ALTER TABLE dbo.Cliente ADD nit VARCHAR(15) NOT NULL CONSTRAINT DF_Cliente_nit DEFAULT 'CF';
GO
-- FIN MIGRACIÓN MÓDULO CLIENTES.

-- =====================================================================
-- DATOS DE PRUEBA: cambios manuales sobre los registros de creates.sql.
-- Solo para una base de pruebas; no los ejecute en la base real.
-- =====================================================================
USE GeovannysBarberDB;
GO


UPDATE Usuario
SET estado = 'Activo'
WHERE id_usuario = 4;

-- Nueva contraseña de DonNoe, guardada con hash BCrypt
UPDATE Usuario
SET contrasena_hash = '$2a$11$XtYlX0hXomMVHBE2u.FbPuoEYs55KZLebm7o5LZTBt9XIRvoBIhdG'
WHERE id_usuario = 2;



UPDATE Trabajador
SET telefono = '5428-9999'
WHERE id_trabajador = 3;

UPDATE Trabajador
SET direccion = 'Zona 8, Quetzaltenango'
WHERE id_trabajador = 5;



UPDATE Asistencia
SET hora_entrada = '08:00:00'
WHERE id_asistencia = 4;

UPDATE Asistencia
SET hora_salida = '17:00:00'
WHERE id_asistencia = 5;



UPDATE Prestamo
SET estado = 'Pagado',
    saldo_pendiente = 0
WHERE id_prestamo = 4;

UPDATE Prestamo
SET estado = 'Autorizado',
    saldo_pendiente = 50.00
WHERE id_prestamo = 2;


UPDATE Cliente
SET telefono = '5552-9090'
WHERE id_cliente = 1;

UPDATE Cliente
SET correo = 'mario.lopez@gmail.com'
WHERE id_cliente = 2;



UPDATE Categoria
SET descripcion = 'Productos para cabello y peinado'
WHERE id_categoria = 1;

UPDATE Categoria
SET nombre = 'Cuidado de barba'
WHERE id_categoria = 2;


UPDATE Producto
SET precio_venta = 95.00
WHERE id_producto = 1;

UPDATE Producto
SET stock = 18
WHERE id_producto = 2;

UPDATE Servicio
SET precio = 110.00
WHERE id_servicio = 1;

UPDATE Servicio
SET descripcion = 'Instalacion completa y ajuste de protesis capilar'
WHERE id_servicio = 4;

UPDATE Movimiento_Inventario
SET motivo = 'Compra mensual al proveedor'
WHERE id_movimiento = 1;

UPDATE Movimiento_Inventario
SET cantidad = 3
WHERE id_movimiento = 2;

UPDATE Factura
SET estado = 'Pagada'
WHERE id_factura = 5;

UPDATE Factura
SET estado = 'Anulada'
WHERE id_factura = 2;

UPDATE Detalle_Producto
SET precio_unitario = 90.00,
    subtotal = 90.00 * cantidad
WHERE id_detalle_producto = 1;

UPDATE Detalle_Producto
SET precio_unitario = 90.00,
    subtotal = 90.00 * cantidad
WHERE id_detalle_producto = 2;

UPDATE Detalle_Servicio
SET precio_unitario = 100.00,
    subtotal = 100.00 * cantidad
WHERE id_detalle_servicio = 1;

UPDATE Detalle_Servicio
SET precio_unitario = 100.00,
    subtotal = 100.00 * cantidad
WHERE id_detalle_servicio = 2;

-- Recalcula el total de las facturas cuyas líneas cambiaron arriba, para que coincida con la suma del detalle.
UPDATE f
SET subtotal = x.suma,
    total = x.suma
FROM Factura f
CROSS APPLY (SELECT
    COALESCE((SELECT SUM(dp.subtotal) FROM Detalle_Producto dp WHERE dp.id_factura = f.id_factura), 0)
  + COALESCE((SELECT SUM(ds.subtotal) FROM Detalle_Servicio ds WHERE ds.id_factura = f.id_factura), 0) AS suma) x
WHERE f.id_factura IN (
    SELECT id_factura FROM Detalle_Producto WHERE id_detalle_producto IN (1, 2)
    UNION
    SELECT id_factura FROM Detalle_Servicio WHERE id_detalle_servicio IN (1, 2));

UPDATE Metodo_Pago
SET nombre = 'Efectivo'
WHERE id_metodo_pago = 1;

UPDATE Metodo_Pago
SET estado = 'Activo'
WHERE id_metodo_pago = 5;

UPDATE Caja
SET estado = 'Cerrada'
WHERE id_caja = 1;

UPDATE Caja
SET nombre = 'Caja auxiliar principal'
WHERE id_caja = 2;

UPDATE Pago
SET estado = 'Completado'
WHERE id_pago = 5;

UPDATE Pago
SET referencia = 'TC-2026-0002'
WHERE id_pago = 2;

UPDATE Transaccion_POS
SET estado = 'Aprobada'
WHERE id_transaccion_pos = 5;

UPDATE Transaccion_POS
SET codigo_autorizacion = 'AUT-2026-1002'
WHERE id_transaccion_pos = 2;

UPDATE Movimiento_Contable
SET concepto = 'Ingreso por venta de productos y servicios'
WHERE id_movimiento = 1;

UPDATE Movimiento_Contable
SET monto = 125.00
WHERE id_movimiento = 2;

UPDATE Cuadre_Caja
SET estado = 'Revisado'
WHERE id_cuadre = 1;

UPDATE Cuadre_Caja
SET monto_real = 620.00,
    diferencia = 0.00
WHERE id_cuadre = 2;

SELECT * FROM Usuario;
SELECT * FROM Trabajador;
SELECT * FROM Asistencia;
SELECT * FROM Prestamo;
SELECT * FROM Cliente;
SELECT * FROM Categoria;
SELECT * FROM Producto;
SELECT * FROM Servicio;
SELECT * FROM Movimiento_Inventario;
SELECT * FROM Factura;
SELECT * FROM Detalle_Producto;
SELECT * FROM Detalle_Servicio;
SELECT * FROM Metodo_Pago;
SELECT * FROM Caja;
SELECT * FROM Pago;
SELECT * FROM Transaccion_POS;
SELECT * FROM Movimiento_Contable;
SELECT * FROM Cuadre_Caja;

