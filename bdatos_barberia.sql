create database barberia

use barberia



CREATE TABLE Usuario (
    id_usuario INT IDENTITY(1,1) PRIMARY KEY,
    nombre_usuario VARCHAR(50),
    contrasena_hash VARCHAR(255),
    rol VARCHAR(30),
    estado VARCHAR(20)
);

CREATE TABLE Trabajador (
    id_trabajador INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT,
    nombres VARCHAR(80),
    apellidos VARCHAR(80),
    telefono VARCHAR(20),
    direccion VARCHAR(200),
    FOREIGN KEY (id_usuario) REFERENCES Usuario(id_usuario)
);
CREATE TABLE Asistencia (
    id_asistencia INT IDENTITY(1,1) PRIMARY KEY,
    id_trabajador INT,
    fecha DATE,
    hora_entrada TIME,
    hora_salida TIME,
    FOREIGN KEY (id_trabajador) REFERENCES Trabajador(id_trabajador)
);

CREATE TABLE Prestamo (
    id_prestamo INT IDENTITY(1,1) PRIMARY KEY,
    id_trabajador INT,
    fecha_solicitud DATE,
    monto DECIMAL(10,2),
    saldo_pendiente DECIMAL(10,2),
    estado VARCHAR(20),
    FOREIGN KEY (id_trabajador) REFERENCES Trabajador(id_trabajador)
);

CREATE TABLE Cliente (
    id_cliente INT IDENTITY(1,1) PRIMARY KEY,
    nombres VARCHAR(100),
    telefono VARCHAR(20),
    correo VARCHAR(100)
);

CREATE TABLE Categoria (
    id_categoria INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(80),
    descripcion VARCHAR(250)
);

CREATE TABLE Producto (
    id_producto INT IDENTITY(1,1) PRIMARY KEY,
    id_categoria INT,
    nombre VARCHAR(100),
    precio_venta DECIMAL(10,2),
    stock INT,
    stock_minimo INT,
    FOREIGN KEY (id_categoria) REFERENCES Categoria(id_categoria)
);

CREATE TABLE Servicio (
    id_servicio INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(100),
    precio DECIMAL(10,2),
    descripcion VARCHAR(250),
    categoria_servicio VARCHAR(80)
);

CREATE TABLE Movimiento_Inventario (
    id_movimiento INT IDENTITY(1,1) PRIMARY KEY,
    id_producto INT,
    tipo VARCHAR(20),
    cantidad INT,
    fecha DATETIME2,
    motivo VARCHAR(250),
    FOREIGN KEY (id_producto) REFERENCES Producto(id_producto)
);

CREATE TABLE Factura (
    id_factura INT IDENTITY(1,1) PRIMARY KEY,
    id_cliente INT,
    id_trabajador INT,
    fecha DATETIME2,
    subtotal DECIMAL(10,2),
    total DECIMAL(10,2),
    estado VARCHAR(20),
    FOREIGN KEY (id_cliente) REFERENCES Cliente(id_cliente),
    FOREIGN KEY (id_trabajador) REFERENCES Trabajador(id_trabajador)
);

CREATE TABLE Detalle_Producto (
    id_detalle_producto INT IDENTITY(1,1) PRIMARY KEY,
    id_factura INT,
    id_producto INT,
    cantidad INT,
    precio_unitario DECIMAL(10,2),
    subtotal DECIMAL(10,2),
    FOREIGN KEY (id_factura) REFERENCES Factura(id_factura),
    FOREIGN KEY (id_producto) REFERENCES Producto(id_producto)
);

CREATE TABLE Detalle_Servicio (
    id_detalle_servicio INT IDENTITY(1,1) PRIMARY KEY,
    id_factura INT,
    id_servicio INT,
    cantidad INT,
    precio_unitario DECIMAL(10,2),
    subtotal DECIMAL(10,2),
    FOREIGN KEY (id_factura) REFERENCES Factura(id_factura),
    FOREIGN KEY (id_servicio) REFERENCES Servicio(id_servicio)
);

CREATE TABLE Metodo_Pago (
    id_metodo_pago INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(50),
    estado VARCHAR(20)
);

CREATE TABLE Caja (
    id_caja INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(80),
    estado VARCHAR(20)
);

CREATE TABLE Pago (
    id_pago INT IDENTITY(1,1) PRIMARY KEY,
    id_factura INT,
    id_metodo_pago INT,
    fecha DATETIME2,
    monto DECIMAL(10,2),
    referencia VARCHAR(100),
    estado VARCHAR(20),
    FOREIGN KEY (id_factura) REFERENCES Factura(id_factura),
    FOREIGN KEY (id_metodo_pago) REFERENCES Metodo_Pago(id_metodo_pago)
);

CREATE TABLE Transaccion_POS (
    id_transaccion_pos INT IDENTITY(1,1) PRIMARY KEY,
    id_pago INT,
    codigo_autorizacion VARCHAR(100),
    referencia_pos VARCHAR(100),
    estado VARCHAR(20),
    fecha_hora DATETIME2,
    FOREIGN KEY (id_pago) REFERENCES Pago(id_pago)
);

CREATE TABLE Movimiento_Contable (
    id_movimiento INT IDENTITY(1,1) PRIMARY KEY,
    id_caja INT,
    id_pago INT,
    tipo VARCHAR(20),
    concepto VARCHAR(200),
    monto DECIMAL(10,2),
    fecha DATETIME2,
    FOREIGN KEY (id_caja) REFERENCES Caja(id_caja),
    FOREIGN KEY (id_pago) REFERENCES Pago(id_pago)
);

CREATE TABLE Cuadre_Caja (
    id_cuadre INT IDENTITY(1,1) PRIMARY KEY,
    id_caja INT,
    fecha_cierre DATETIME2,
    monto_esperado DECIMAL(10,2),
    monto_real DECIMAL(10,2),
    diferencia DECIMAL(10,2),
    estado VARCHAR(20),
    FOREIGN KEY (id_caja) REFERENCES Caja(id_caja)
);