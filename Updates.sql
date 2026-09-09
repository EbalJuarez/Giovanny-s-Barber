USE barberia;


UPDATE Usuario
SET estado = 'Activo'
WHERE id_usuario = 4;

UPDATE Usuario
SET contrasena_hash = 'NuevaClaveNoe2026*'
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

