function soloNumeros(id, maximo = 12) {
    const input = document.getElementById(id);
    if (!input) return;

    input.addEventListener("input", function () {
        this.value = this.value.replace(/\D/g, "").substring(0, maximo);
    });
}

function soloDecimales(id, enterosMaximos = 9, decimalesMaximos = 2) {
    const input = document.getElementById(id);
    if (!input) return;

    input.setAttribute("inputmode", "decimal");
    input.addEventListener("input", function () {
        let valor = this.value.replace(",", ".").replace(/[^0-9.]/g, "");
        const partes = valor.split(".");
        const entero = (partes.shift() || "").substring(0, enterosMaximos);
        const decimal = partes.join("").substring(0, decimalesMaximos);
        this.value = partes.length > 0 ? `${entero}.${decimal}` : entero;
    });
}

function soloLetras(id, maximo) {
    const input = document.getElementById(id);
    if (!input) return;

    input.addEventListener("input", function () {
        this.value = this.value
            .replace(/[^a-zA-ZáéíóúÁÉÍÓÚñÑ\s]/g, "")
            .substring(0, maximo);
    });
}

document.addEventListener("DOMContentLoaded", function () {
    soloLetras("Nombre", 50);
    soloLetras("Apellidos", 50);
    soloNumeros("PhoneNumber", 8);

    soloNumeros("Cedula", 12);
    soloNumeros("Telefono", 8);
    soloLetras("Canton", 80);
    soloLetras("Distrito", 80);

    ["Precio", "Cantidad", "PesoKg", "CantidadProcesadaKg", "CantidadResultanteKg", "Monto", "Subtotal", "IVA", "Total", "Stock", "StockMinimo"]
        .forEach(id => soloDecimales(id));

    soloLetras("Responsable", 100);
    soloLetras("ClienteNombre", 100);
    soloNumeros("ClienteTelefono", 8);

    const form = document.querySelector("form");
    const password = document.getElementById("usuarioPassword");
    const confirmPassword = document.getElementById("usuarioConfirmPassword");

    if (form && password && confirmPassword) {
        form.addEventListener("submit", function (e) {
            const regex = /^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[\W_]).{8,}$/;

            if (!regex.test(password.value)) {
                Swal.fire({
                    icon: "warning",
                    title: "Contraseña inválida",
                    text: "Debe contener mínimo 8 caracteres, mayúscula, minúscula, número y símbolo."
                });
                e.preventDefault();
                return;
            }

            if (password.value !== confirmPassword.value) {
                Swal.fire({
                    icon: "warning",
                    title: "Contraseñas diferentes",
                    text: "Las contraseñas no coinciden."
                });
                e.preventDefault();
            }
        });
    }
});

function toggleUsuarioPassword(inputId, iconId) {
    const input = document.getElementById(inputId);
    const icon = document.getElementById(iconId);
    if (!input || !icon) return;

    const mostrar = input.type === "password";
    input.type = mostrar ? "text" : "password";
    icon.classList.toggle("fa-eye", !mostrar);
    icon.classList.toggle("fa-eye-slash", mostrar);
}
