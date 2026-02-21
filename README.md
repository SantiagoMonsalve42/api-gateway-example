# API Gateway Example

Proyecto de ejemplo que demuestra un API Gateway usando **Ocelot** para exponer microservicios desarrollados en diferentes tecnologías (Node.js, Go, Python y Java).

## 🏗️ Arquitectura

```
┌─────────────────────────────────────────────────────────┐
│                    API GATEWAY (Ocelot)                 │
│                   http://localhost:4242                 │
└────────────┬──────────────┬──────────────┬──────────────┘
             │              │              │
    ┌────────▼─────┐  ┌─────▼──────┐  ┌──▼───────┐  ┌──────────┐
    │ service-node │  │ service-go │  │ service- │  │ service- │
    │   (Node.js)  │  │    (Go)    │  │  python  │  │  java    │
    │ :3000        │  │ :8080      │  │ :8000    │  │ :8080    │
    └──────────────┘  └────────────┘  └──────────┘  └──────────┘
```

## 📋 Rutas Disponibles

| Endpoint | Método | Servicio | Puerto |
|----------|--------|----------|--------|
| `/v1/users` | GET | service-node | 3000 |
| `/v1/orders` | GET | service-go | 8080 |
| `/v1/files` | GET | service-python | 8000 |
| `/v1/sales` | GET | service-java | 8080 |

## 🚀 Cómo ejecutar

### Requisitos previos

- Docker instalado
- Docker Compose instalado
- Git (opcional)

### Paso 1: Clonar el repositorio

```bash
git clone <repositorio>
cd api-gateway-example
```

### Paso 2: Iniciar los servicios

```bash
docker-compose up -d
```

Este comando va a:
- Descargar las imágenes base necesarias
- Compilar la imagen del API Gateway
- Compilar las imágenes de todos los servicios
- Crear una red de Docker compartida
- Iniciar todos los contenedores en segundo plano

**Tiempo estimado:** 2-3 minutos en la primera ejecución

### Paso 3: Verificar que los servicios estén listos

```bash
docker-compose ps
```

Deberías ver algo como:

```
NAME           STATUS
api-gateway    Up (healthy)
service-node   Up
service-go     Up
service-python Up
service-java   Up
```

### Paso 4: Testear los endpoints

#### A través del Gateway (recomendado):

```bash
# Obtener usuarios
curl http://localhost:4242/v1/users

# Obtener órdenes
curl http://localhost:4242/v1/orders

# Obtener archivos
curl http://localhost:4242/v1/files

# Obtener ventas
curl http://localhost:4242/v1/sales
```

#### Directamente desde el contenedor (interno):

```bash
docker exec api-gateway curl http://service-python:8000/files
```

## 🛑 Detener los servicios

```bash
docker-compose down
```

Esto detiene y elimina todos los contenedores (los volúmenes de datos se mantienen).

Para eliminar todo incluidos volúmenes:

```bash
docker-compose down -v
```

## 🔍 Solución de problemas

### Ver logs de un servicio

```bash
# Logs del gateway
docker logs api-gateway

# Logs de Python
docker logs api-gateway-service-python

# Ver logs en tiempo real
docker logs -f api-gateway
```

### Verificar conectividad entre servicios

```bash
# Desde el gateway
docker exec api-gateway curl http://service-python:8000/files

# Desde otro contenedor
docker exec <nombre-contenedor> ping service-python
```

### Reconstruir las imágenes

Si hiciste cambios en el código:

```bash
docker-compose up -d --build
```

## 📂 Estructura del proyecto

```
api-gateway-example/
├── gateway/                    # API Gateway (Ocelot, .NET 8)
│   ├── Dockerfile
│   └── ApiGateway/
│       ├── ocelot.json        # Configuración de rutas
│       └── Program.cs
├── services/
│   ├── service-node/          # Servicio en Node.js
│   ├── service-go/            # Servicio en Go
│   ├── service-python/        # Servicio en Python
│   └── service-java/          # Servicio en Java
├── docker-compose.yml         # Orquestación de contenedores
└── README.md
```

## ⚙️ Configuración de Ocelot

La configuración de rutas se encuentra en [gateway/ApiGateway/ocelot.json](gateway/ApiGateway/ocelot.json).

Ejemplo de cómo añadir una nueva ruta:

```json
{
  "UpstreamPathTemplate": "/v1/nueva-ruta",
  "UpstreamHttpMethod": [ "GET" ],
  "DownstreamPathTemplate": "/nueva-ruta",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [
    { "Host": "nuevo-servicio", "Port": 3000 }
  ]
}
```

## 🔗 Enlaces útiles

- [Ocelot Documentation](https://ocelotnetwork.readthedocs.io/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- Servicios individuales en sus respectivas README.md

## 📝 Notas

- Todos los servicios están en una red `demo-network` privada
- El gateway es el único punto de entrada público (puerto 4242)
- Los puertos internos de los servicios no se exponen al host


