const email = document.getElementById("email");
const password = document.getElementById("password");
const confirmPassword = document.getElementById("confirmPassword");

function validate() {

    if (email) {

        const emailValid =
            /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value);

        toggle("emailMessage", emailValid);

    }

    if (password) {

        const value = password.value;

        toggle("length", value.length >= 8);
        toggle("uppercase", /[A-Z]/.test(value));
        toggle("lowercase", /[a-z]/.test(value));
        toggle("number", /[0-9]/.test(value));
        toggle("symbol", /[^A-Za-z0-9]/.test(value));

    }

    if (password && confirmPassword) {

        const match =
            password.value === confirmPassword.value &&
            password.value.length > 0;

        toggle("match", match);

    }
}
function togglePassword(inputId, iconId) {
    const input = document.getElementById(inputId);
    const icon = document.getElementById(iconId);

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
const emailInput = document.getElementById("email");
const form = document.querySelector("#registerForm");

if (form) {
    form.addEventListener("submit", function (e) {
        // La confirmación de registro depende SIEMPRE de la respuesta del servidor.
        // No mostrar "Revisá tu correo" antes de comprobar correo duplicado,
        // campos requeridos y creación real de la cuenta.
        if (!form.reportValidity()) {
            e.preventDefault();
            return;
        }

        const passwordField = document.getElementById("password");
        const confirmPasswordField = document.getElementById("confirmPassword");

        if (passwordField && confirmPasswordField) {
            const value = passwordField.value;
            const validPassword =
                value.length >= 8 &&
                /[A-Z]/.test(value) &&
                /[a-z]/.test(value) &&
                /[0-9]/.test(value) &&
                /[^A-Za-z0-9]/.test(value);

            const match = passwordField.value === confirmPasswordField.value;

            if (!validPassword || !match) {
                e.preventDefault();
                validate();
                return;
            }
        }

        // Si todo es válido en cliente, el POST continúa normalmente.
        // ASP.NET Identity decide si el correo ya existe y solo cuando la
        // creación es exitosa redirige a la pantalla de confirmación.
    });
}

function toggle(id, valid) {

    const element = document.getElementById(id);

    if (!element) return;

    const icon = element.querySelector("i");

    if (valid) {

        element.style.color = "#00ff99";

        icon.classList.remove("fa-circle-xmark");
        icon.classList.add("fa-circle-check");

        icon.style.color = "#00ff99";

    } else {

        element.style.color = "#4a2c1a";

        icon.classList.remove("fa-circle-check");
        icon.classList.add("fa-circle-xmark");

        icon.style.color = "#ff4d4d";
    }

}

if (email)
    email.addEventListener("keyup", validate);

if (password)
    password.addEventListener("keyup", validate);

if (confirmPassword)
    confirmPassword.addEventListener("keyup", validate);