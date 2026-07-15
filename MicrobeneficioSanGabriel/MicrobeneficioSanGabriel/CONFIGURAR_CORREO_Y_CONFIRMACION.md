# Confirmación por correo real

El proyecto está configurado para enviar correos reales desde Gmail mediante SMTP.
No incluye un modo de confirmación local y no muestra un botón para saltarse el correo.

## Único paso obligatorio en cada computadora

1. Active la verificación en dos pasos de la cuenta remitente.
2. Cree una contraseña de aplicación de Google de 16 caracteres.
3. Ejecute `CONFIGURAR_CORREO_GMAIL.bat` dentro de la carpeta `App`.
4. Ingrese el correo remitente y la contraseña de aplicación cuando se soliciten.
5. Cierre y vuelva a abrir Visual Studio.
6. Ejecute el proyecto y use **Reenviar correo** para cualquier cuenta pendiente.

La configuración usa:

- Servidor: `smtp.gmail.com`
- Puerto: `587`
- SSL/TLS: activado
- Remitente predeterminado: `soportemicrobeneficiosg@gmail.com`

La contraseña se guarda con **User Secrets**, no dentro del ZIP ni de `appsettings.json`.

## Resultado esperado

El destinatario recibe un mensaje HTML con:

- Nombre **Microbeneficio San Gabriel**.
- Texto de confirmación.
- Botón **Confirmar correo**.
- Imagen institucional al pie.

Al presionar el botón, la cuenta queda confirmada y puede iniciar sesión.
