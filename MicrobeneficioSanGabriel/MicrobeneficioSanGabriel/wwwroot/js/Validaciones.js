function soloNumeros(id, maximo) {

    const input = document.getElementById(id);

    if (!input) return;

    input.addEventListener("input", function () {

        this.value = this.value
            .replace(/\D/g, '')
            .substring(0, maximo);
    });
}

function soloDecimal(id, maxEnteros = 9, maxDecimales = 2) {

    const input = document.getElementById(id);

    if (!input) return;

    input.setAttribute("inputmode", "decimal");
    input.setAttribute("step", "0.01");

    input.addEventListener("input", function () {

        let valor = this.value
            .replace(/,/g, '.')
            .replace(/[^0-9.]/g, '');

        const partes = valor.split('.');
        let enteros = (partes[0] || '').substring(0, maxEnteros);
        let decimales = partes.slice(1).join('').substring(0, maxDecimales);

        if (valor.startsWith('.')) {
            enteros = '0';
        }

        this.value = partes.length > 1 ? `${enteros}.${decimales}` : enteros;
    });
}

function soloLetras(id, maximo) {

    const input = document.getElementById(id);

    if (!input) return;

    input.addEventListener("input", function () {

        this.value = this.value
            .replace(/[^a-zA-ZáéíóúÁÉÍÓÚñÑ\s]/g, '')
            .substring(0, maximo);
    });
}

document.addEventListener("DOMContentLoaded", function () {

    // Usuarios
    soloLetras("Nombre", 50);
    soloLetras("Apellidos", 25);
    soloNumeros("PhoneNumber", 8);

    // Productores
    soloNumeros("Cedula", 9);
    soloNumeros("Telefono", 8);
    soloLetras("Finca", 11)
    soloDecimal("Precio", 9, 2)
    soloLetras("Canton", 12)
    soloLetras("Distrito", 12)
    
    //Inventario
    soloDecimal("Cantidad", 9, 2)

    //Lote
    soloDecimal("PesoKg", 9, 2)

    //Producción
    soloDecimal("CantidadProcesadaKg", 9, 2)
    soloDecimal("CantidadResultanteKg", 9, 2)

    //Trazabilidad
    soloLetras("Responsable", 10)

    //Pedidos
    soloLetras("ClienteNombre", 35)
    soloNumeros("ClienteTelefono", 8)
    soloDecimal("Cantidad", 9, 2)

    //Movimiento financiero
    soloDecimal("Monto", 9, 2)

    //Facturas
    soloDecimal("Subtotal", 9, 2)
    soloDecimal("IVA", 9, 2)
    soloDecimal("Total", 9, 2)

    //Productos
    soloDecimal("Stock", 9, 2)
    soloDecimal("StockMinimo", 9, 2)


});

document.querySelector("form")
    .addEventListener("submit", function (e) {

        const password =
            document.getElementById("usuarioPassword").value;

        const confirmPassword =
            document.getElementById("usuarioConfirmPassword").value;

        const regex =
            /^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$/;

        if (!regex.test(password)) {

            Swal.fire({
                icon: 'warning',
                title: 'Contraseña inválida',
                text: 'Debe contener mínimo 8 caracteres, una mayúscula, un número y un símbolo.'
            });

            e.preventDefault();
            return;
        }

        if (password !== confirmPassword) {

            Swal.fire({
                icon: 'warning',
                title: 'Contraseñas diferentes',
                text: 'Las contraseñas no coinciden.'
            });

            e.preventDefault();
        }
    });
function toggleUsuarioPassword(inputId, iconId) {
    const input = document.getElementById(inputId);
    const icon = document.getElementById(iconId);

    if (!input || !icon) {
        return;
    }

    if (input.type === "password") {
        input.type = "text";
        icon.classList.remove("fa-eye");
        icon.classList.add("fa-eye-slash");
    } else {
        input.type = "password";
        icon.classList.remove("fa-eye-slash");
        icon.classList.add("fa-eye");
    }
}