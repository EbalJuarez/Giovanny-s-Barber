USE barberia;
-- 1. eliminar el usuario de prueba
DELETE FROM Usuario
WHERE nombre_usuario = 'UsuarioPruebaDELETE';

-- 2. eliminar el cliente de prueba
DELETE FROM Cliente
WHERE correo = 'cliente.delete@prueba.com';

-- 3. eliminar la categoria de prueba
DELETE FROM Categoria
WHERE nombre = 'Categoria DELETE';

-- 4. eliminar el producto de prueba
DELETE FROM Producto
WHERE nombre = 'Producto de prueba DELETE';

-- 5. eliminar el servicio de prueba
DELETE FROM Servicio
WHERE nombre = 'Servicio de prueba DELETE';

-- 6. eliminar el metodo de pago de prueba
DELETE FROM Metodo_Pago
WHERE nombre = 'Metodo DELETE';

-- 7. eliminar la caja de prueba
DELETE FROM Caja
WHERE nombre = 'Caja de prueba DELETE';

-- 8. eliminar la asistencia creada por error
DELETE FROM Asistencia
WHERE id_trabajador = 1
AND fecha = '2026-09-01';

-- 9. eliminar el prestamo creado por error
DELETE FROM Prestamo
WHERE id_trabajador = 1
AND fecha_solicitud = '2026-09-01'
AND estado = 'Prueba';

-- 10. eliminar el movimiento de inventario de prueba
DELETE FROM Movimiento_Inventario
WHERE motivo = 'Movimiento de prueba para DELETE';
GO

select * From Movimiento_Inventario