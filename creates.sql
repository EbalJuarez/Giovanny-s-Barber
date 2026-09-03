USE barberia;

-- 1. Usuario
INSERT INTO Usuario (nombre_usuario, contrasena_hash, rol, estado) VALUES
('DonGio', 'Gato1979*#', 'Administrador', 'Activo'),
('DonNoe', 'hash_noe123', 'Trabajador', 'Activo'),
('Lis', 'hash_lis123', 'Cajera', 'Activo'),
('Alex', 'hash_alex123', 'Trabajador', 'Inactivo'),
('Willy', 'hash_willi123', 'Trabajador', 'Inactivo');

-- 2. Trabajador
INSERT INTO Trabajador (id_usuario, nombres, apellidos, telefono, direccion) VALUES
(1, 'Geovanny', 'Orozco', '4199-3840', 'Zona 3, Quetzaltenango'),
(2, 'Noe', 'Ramirez', '5551-2332', 'Olintepeque, Quetzaltenango'),
(3, 'Lis', 'Rodas', '5428-3840', 'Zona 3, Quetzaltenango'),
(4, 'Alex', 'Fuentes', '4418-2884', 'Zona 9, Quetzaltenango'),
(5, 'Willy', 'Cifuentes', '8554-1247', 'Zona 7, Quetzaltenango');

-- 3. Asistencia
INSERT INTO Asistencia (id_trabajador, fecha, hora_entrada, hora_salida) VALUES
(1, '2026-08-25', '08:00:00', '17:00:00'),
(2, '2026-08-25', '08:05:00', '17:10:00'),
(3, '2026-08-25', '07:55:00', '16:55:00'),
(4, '2026-08-26', '08:10:00', '17:05:00'),
(5, '2026-08-26', '08:00:00', '16:50:00');

-- 4. Prestamo
INSERT INTO Prestamo (id_trabajador, fecha_solicitud, monto, saldo_pendiente, estado) VALUES
(1, '2026-07-10', 150.00,50.00, 'Autorizado'),
(2, '2026-07-15', 80.00, 50.00, 'Pendiente'),
(3, '2026-08-01', 200.00, 100.00, 'Autorizado'),
(4, '2026-08-05', 50.00, 20.00, 'Pagado'),
(5, '2026-08-10', 100.00, 100.00, 'Rechazado');

-- 5. Cliente
INSERT INTO Cliente (nombres, telefono, correo) VALUES
('Luis Hernandez', '5552-2001', 'luis@gmail.com'),
('Mario Lopez', '5552-2002', 'mario@gmail.com'),
('Daniela Garcia', '5552-2003', 'daniela@gmail.com'),
('Fernando Ruiz', '5552-2004', 'fernando@gmail.com'),
('Sofia Morales', '5552-2005', 'sofia@gmail.com');

-- 6. Categoria
INSERT INTO Categoria (nombre, descripcion) VALUES
('Cabello', 'Productos para el cuidado del cabello'),
('Barba', 'Productos para barba y bigote'),
('Higiene', 'Productos de limpieza personal'),
('Accesorios', 'Accesorios utilizados en barberia'),
('Tratamientos', 'Productos para tratamientos capilares');

-- 7. Producto
INSERT INTO Producto (id_categoria, nombre, precio_venta, stock, stock_minimo) VALUES
(1, 'Gel para cabello', 90.00, 25, 5),
(1, 'Cera moldeadora', 90.00, 20, 5),
(2, 'Aceite para barba', 90.00, 15, 4),
(3, 'Shampoo profesional', 90.00, 12, 3),
(4, 'Peine para barba', 35.00, 30, 6);

-- 8. Servicio
INSERT INTO Servicio (nombre, precio, descripcion, categoria_servicio) VALUES
('Corte de cabello', 100.00, 'Corte de cabello para caballero', 'Servicio Comun'),
('Corte de barba', 100.00, 'Corte y detallado de barba', 'Servicio Comun'),
('Corte de cabello y barba', 190.00, 'Servicio combinado de corte de cabello y barba', 'Servicio Comun'),
('Instalacion de protesis capilar', 3500.00, 'Servicio de instalacion de protesis capilar', 'Protesis Capilares'),
('Mantenimiento de protesis capilar', 300.00, 'Servicio de limpieza y mantenimiento a la protesis capilar', 'Protesis capilares');

-- 9. Movimiento_Inventario
INSERT INTO Movimiento_Inventario (id_producto, tipo, cantidad, fecha, motivo) VALUES
(1, 'Entrada', 10, '2026-08-20 09:00:00', 'Compra al proveedor'),
(2, 'Salida', 2, '2026-08-21 11:30:00', 'Venta de producto'),
(3, 'Entrada', 5, '2026-08-22 10:15:00', 'Reposicion de inventario'),
(4, 'Salida', 1, '2026-08-23 15:20:00', 'Uso interno'),
(5, 'Ajuste', 3, '2026-08-24 16:00:00', 'Correccion de inventario');

-- 10. Factura
INSERT INTO Factura (id_cliente, id_trabajador, fecha, subtotal, total, estado) VALUES
(1, 1, '2026-08-25 09:30:00', 95.00, 95.00, 'Pagada'),
(2, 2, '2026-08-25 11:00:00', 125.00, 125.00, 'Pagada'),
(3, 3, '2026-08-25 14:15:00', 115.00, 115.00, 'Pagada'),
(4, 1, '2026-08-26 10:20:00', 180.00, 180.00, 'Pagada'),
(5, 2, '2026-08-26 16:40:00', 100.00, 100.00, 'Emitida');

-- 11. Detalle_Producto
INSERT INTO Detalle_Producto (id_factura, id_producto, cantidad, precio_unitario, subtotal) VALUES
(1, 1, 1, 45.00, 45.00),
(2, 2, 1, 60.00, 60.00),
(3, 3, 1, 75.00, 75.00),
(4, 4, 1, 90.00, 90.00),
(5, 5, 1, 35.00, 35.00);

-- 12. Detalle_Servicio
INSERT INTO Detalle_Servicio (id_factura, id_servicio, cantidad, precio_unitario, subtotal) VALUES
(1, 1, 1, 50.00, 50.00),
(2, 2, 1, 65.00, 65.00),
(3, 3, 1, 40.00, 40.00),
(4, 4, 1, 90.00, 90.00),
(5, 2, 1, 65.00, 65.00);

-- 13. Metodo_Pago
INSERT INTO Metodo_Pago (nombre, estado) VALUES
('Efectivo', 'Activo'),
('Tarjeta de credito', 'Activo'),
('Tarjeta de debito', 'Activo'),
('Transferencia', 'Activo'),
('Pago QR', 'Activo');

-- 14. Caja
INSERT INTO Caja (nombre, estado) VALUES
('Caja principal', 'Abierta'),
('Caja secundaria', 'Abierta'),
('Caja recepcion', 'Cerrada'),
('Caja sucursal centro', 'Abierta'),
('Caja sucursal zona 3', 'Cerrada');

-- 15. Pago
INSERT INTO Pago (id_factura, id_metodo_pago, fecha, monto, referencia, estado) VALUES
(1, 1, '2026-08-25 09:35:00', 95.00, 'EF-0001', 'Completado'),
(2, 2, '2026-08-25 11:05:00', 125.00, 'TC-0002', 'Completado'),
(3, 3, '2026-08-25 14:20:00', 115.00, 'TD-0003', 'Completado'),
(4, 4, '2026-08-26 10:25:00', 180.00, 'TR-0004', 'Completado'),
(5, 5, '2026-08-26 16:45:00', 100.00, 'QR-0005', 'Pendiente');

-- 16. Transaccion_POS
INSERT INTO Transaccion_POS (id_pago, codigo_autorizacion, referencia_pos, estado, fecha_hora) VALUES
(1, 'AUT-1001', 'POS-0001', 'Aprobada', '2026-08-25 09:35:00'),
(2, 'AUT-1002', 'POS-0002', 'Aprobada', '2026-08-25 11:05:00'),
(3, 'AUT-1003', 'POS-0003', 'Aprobada', '2026-08-25 14:20:00'),
(4, 'AUT-1004', 'POS-0004', 'Aprobada', '2026-08-26 10:25:00'),
(5, 'AUT-1005', 'POS-0005', 'Pendiente', '2026-08-26 16:45:00');

-- 17. Movimiento_Contable
INSERT INTO Movimiento_Contable (id_caja, id_pago, tipo, concepto, monto, fecha) VALUES
(1, 1, 'Ingreso', 'Pago de factura 1', 95.00, '2026-08-25 09:35:00'),
(1, 2, 'Ingreso', 'Pago de factura 2', 125.00, '2026-08-25 11:05:00'),
(2, 3, 'Ingreso', 'Pago de factura 3', 115.00, '2026-08-25 14:20:00'),
(1, 4, 'Ingreso', 'Pago de factura 4', 180.00, '2026-08-26 10:25:00'),
(2, 5, 'Ingreso', 'Pago de factura 5', 100.00, '2026-08-26 16:45:00');

-- 18. Cuadre_Caja
INSERT INTO Cuadre_Caja (id_caja, fecha_cierre, monto_esperado, monto_real, diferencia, estado) VALUES
(1, '2026-08-22 18:00:00', 850.00, 850.00, 0.00, 'Cerrado'),
(2, '2026-08-23 18:00:00', 620.00, 615.00, -5.00, 'Revisado'),
(3, '2026-08-24 18:00:00', 430.00, 440.00, 10.00, 'Revisado'),
(4, '2026-08-25 18:00:00', 980.00, 980.00, 0.00, 'Cerrado'),
(5, '2026-08-26 18:00:00', 700.00, 695.00, -5.00, 'Cerrado');
