namespace MicrobeneficioSanGabriel.Services
{
    public class EmailTemplate
    {
        public static string BaseTemplate(
            string title,
            string message,
            string buttonText,
            string buttonUrl)
        {
            return $@"

<div style='
    font-family:Poppins,Arial,sans-serif;
    max-width:600px;
    margin:auto;
    padding:40px;
    background:#f8f5e9;
    border-radius:18px;
    border:1px solid #e8dcc2;'>

    <h1 style='
        color:#5c3317;
        text-align:center;'>

        Microbeneficio San Gabriel

    </h1>

    <p style='
        color:#555;
        font-size:15px;'>

        {message}

    </p>

    <div style='
        text-align:center;
        margin:35px 0;'>

        <a href='{buttonUrl}'
           style='
                background:#5c3317;
                color:white;
                padding:15px 30px;
                border-radius:12px;
                text-decoration:none;
                font-weight:bold;
                display:inline-block;'>

            {buttonText}

        </a>

    </div>

    <img src='https://raw.githubusercontent.com/Kevinscr2345/Interfaz_Proyecto/main/EmailFooter.png'
         style='
            width:75%;
            display:block;
            margin:auto;
            border-radius:12px;'>

</div>";

        }
    }
}
