using System.Net;

namespace MicrobeneficioSanGabriel.Services
{
    public static class EmailTemplate
    {
        private const string FooterImageUrl =
            "https://raw.githubusercontent.com/Kevinscr2345/Interfaz_Proyecto/main/EmailFooter.png";

        /// <summary>
        /// Plantilla compatible con Gmail para confirmación de cuenta y recuperación de contraseña.
        /// Conserva el diseño beige, el botón café y la imagen institucional utilizada anteriormente.
        /// </summary>
        public static string BaseTemplate(
            string title,
            string message,
            string buttonText,
            string buttonUrl)
        {
            var safeTitle = WebUtility.HtmlEncode(title);
            var safeButtonText = WebUtility.HtmlEncode(buttonText);

            // message y buttonUrl llegan codificados desde las páginas de Identity.
            return $"""
                <!doctype html>
                <html lang="es">
                <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1">
                    <title>{safeTitle}</title>
                </head>
                <body style="margin:0;padding:28px 12px;background:#ffffff;font-family:Arial,Helvetica,sans-serif;color:#3f3129;">
                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                        <tr>
                            <td align="center">
                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0"
                                       style="max-width:680px;background:#f8f5e9;border:1px solid #e8dcc2;border-radius:18px;overflow:hidden;">
                                    <tr>
                                        <td style="padding:54px 42px 24px;text-align:center;">
                                            <h1 style="margin:0;color:#5c3317;font-size:29px;line-height:1.25;font-weight:800;">
                                                Microbeneficio San Gabriel
                                            </h1>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="padding:0 42px;text-align:left;color:#555555;font-size:16px;line-height:1.65;">
                                            {message}
                                        </td>
                                    </tr>
                                    <tr>
                                        <td align="center" style="padding:34px 42px;">
                                            <a href="{buttonUrl}"
                                               style="display:inline-block;background:#6b3514;color:#ffffff;text-decoration:none;font-size:16px;font-weight:700;padding:15px 31px;border-radius:12px;">
                                                {safeButtonText}
                                            </a>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td align="center" style="padding:0 42px 34px;">
                                            <img src="{FooterImageUrl}"
                                                 alt="Microbeneficio San Gabriel"
                                                 width="450"
                                                 style="display:block;width:100%;max-width:450px;height:auto;border:0;border-radius:12px;">
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="padding:0 42px 40px;text-align:center;color:#806f64;font-size:12px;line-height:1.5;">
                                            Si no solicitaste este mensaje, podés ignorarlo de forma segura.
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>
                """;
        }
    }
}
