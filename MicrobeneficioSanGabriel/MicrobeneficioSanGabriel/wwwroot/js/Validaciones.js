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
                    html: '<div class="sgl-swal-single-msg">Debe contener mínimo 8 caracteres, mayúscula, minúscula, número y símbolo.</div>',
                    confirmButtonText: '<i class="fa-solid fa-check me-2"></i>Entendido',
                    customClass: {
                        popup: "sgl-swal-popup",
                        title: "sgl-swal-title",
                        confirmButton: "sgl-swal-confirm-btn"
                    },
                    buttonsStyling: false
                });
                e.preventDefault();
                return;
            }

            if (password.value !== confirmPassword.value) {
                Swal.fire({
                    icon: "warning",
                    title: "Contraseñas diferentes",
                    html: '<div class="sgl-swal-single-msg">Las contraseñas no coinciden.</div>',
                    confirmButtonText: '<i class="fa-solid fa-check me-2"></i>Entendido',
                    customClass: {
                        popup: "sgl-swal-popup",
                        title: "sgl-swal-title",
                        confirmButton: "sgl-swal-confirm-btn"
                    },
                    buttonsStyling: false
                });
                e.preventDefault();
            }
        });
    }
});

function sglTogglePassword(inputId, iconId) {
    const input = document.getElementById(inputId);
    const icon = document.getElementById(iconId);
    if (!input || !icon) return;

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

function sglSetRule(id, isValid) {
    const row = document.getElementById(id);
    if (!row) return;
    const icon = row.querySelector("i");

    row.classList.toggle("sgl-rule--ok", isValid);
    if (icon) {
        icon.classList.toggle("fa-circle-check", isValid);
        icon.classList.toggle("fa-circle-xmark", !isValid);
    }
}

function sglValidateRegisterForm() {
    const email = document.getElementById("email")?.value.trim() ?? "";
    const password = document.getElementById("password")?.value ?? "";
    const confirmPassword = document.getElementById("confirmPassword")?.value ?? "";

    const emailIsValid = /^[^\s@]+@[^\s@]+\.[A-Za-z]{2,}$/.test(email);
    sglSetRule("emailMessage", emailIsValid);
    sglSetRule("length", password.length >= 8);
    sglSetRule("uppercase", /[A-Z]/.test(password));
    sglSetRule("lowercase", /[a-z]/.test(password));
    sglSetRule("number", /[0-9]/.test(password));
    sglSetRule("symbol", /[^A-Za-z0-9]/.test(password));
    sglSetRule("match", password.length > 0 && password === confirmPassword);
}

document.addEventListener("DOMContentLoaded", function () {
    ["email", "password", "confirmPassword"].forEach(function (id) {
        const el = document.getElementById(id);
        if (el) el.addEventListener("input", sglValidateRegisterForm);
    });
});


// Registro público: segunda barrera de cliente para impedir campos vacíos o solo espacios.
document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("registerForm");
    if (!form) return;

    const field = name => form.querySelector(`[name="Input.${name}"]`);
    const nombre = field("Nombre");
    const apellidos = field("Apellidos");
    const telefono = field("PhoneNumber");
    const email = field("Email");
    const password = field("Password");
    const confirmPassword = field("ConfirmPassword");

    [nombre, apellidos, email].forEach(input => {
        input?.addEventListener("blur", () => {
            input.value = input.value.trim();
        });
    });

    form.addEventListener("submit", function (event) {
        const errores = [];
        const correo = (email?.value || "").trim();
        const telefonoLimpio = (telefono?.value || "").replace(/\D/g, "");

        if (!(nombre?.value || "").trim()) errores.push("El nombre es obligatorio.");
        if (!(apellidos?.value || "").trim()) errores.push("Los apellidos son obligatorios.");
        if (telefonoLimpio.length !== 8) errores.push("El teléfono debe contener exactamente 8 dígitos.");
        if (!/^[^\s@]+@[^\s@]+(?:\.[A-Za-z]{2,})+$/.test(correo)) errores.push("Ingrese un correo electrónico completo y válido.");
        if (!(password?.value || "").trim()) errores.push("La contraseña es obligatoria.");
        if (!(confirmPassword?.value || "").trim()) errores.push("Debe confirmar la contraseña.");
        if ((password?.value || "") !== (confirmPassword?.value || "")) errores.push("Las contraseñas no coinciden.");

        if (errores.length === 0) return;

        event.preventDefault();
        event.stopPropagation();

        if (window.Swal) {
            let htmlContent = "";
            if (errores.length === 1) {
                htmlContent = `<div class="sgl-swal-single-msg">${errores[0]}</div>`;
            } else {
                htmlContent = `
                    <div class="sgl-swal-list-wrapper">
                        <p class="sgl-swal-subtitle">Por favor complete o corrija los siguientes campos:</p>
                        <ul class="sgl-swal-error-list">
                            ${errores.map(e => `<li><i class="fa-solid fa-circle-exclamation"></i><span>${e}</span></li>`).join("")}
                        </ul>
                    </div>`;
            }

            Swal.fire({
                icon: "warning",
                title: "Revise el formulario",
                html: htmlContent,
                confirmButtonText: '<i class="fa-solid fa-check me-2"></i>Entendido',
                customClass: {
                    popup: "sgl-swal-popup",
                    title: "sgl-swal-title",
                    confirmButton: "sgl-swal-confirm-btn"
                },
                buttonsStyling: false
            });
        } else {
            alert(errores.join("\n"));
        }
    }, true);
});
