function soloNumeros(id, maximo) {

    const input = document.getElementById(id);

    if (!input) return;

    input.addEventListener("input", function () {

        this.value = this.value
            .replace(/\D/g, '')
            .substring(0, maximo);
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
    soloLetras("Nombre", 12);
    soloLetras("Apellidos", 25);
    soloNumeros("PhoneNumber", 8);

    // Productores
    soloLetras("NombreProductor", 50);
    soloNumeros("Cedula", 9);
    soloNumeros("Telefono", 8);
    soloLetras("Finca", 11)
    soloNumeros("Precio", 5)

    //Inventario
    soloNumeros("Cantidad", 5)

    //Lote
    soloNumeros("PesoKg", 5)

    //Producción
    soloNumeros("CantidadProcesadaKg", 5)
    soloNumeros("CantidadResultanteKg", 5)

    //Trazabilidad
    soloLetras("Responsable", 10)

    //Pedidos
    soloLetras("ClienteNombre", 35)
    soloNumeros("ClienteTelefono", 8)
    soloNumeros("Cantidad", 5)

    //Movimiento finacniero
    soloNumeros("Monto")

    //Facturas
    soloNumeros("Subtotal")
    soloNumeros("IVA")
    soloNumeros("Total")


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