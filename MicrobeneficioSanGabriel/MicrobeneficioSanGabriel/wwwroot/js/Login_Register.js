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

const form = document.querySelector("#registerForm");
if (form) {
    form.addEventListener("submit", async function (e) {
        e.preventDefault();
        await Swal.fire({
            icon: 'success',
            title: 'Revisá tu correo',
            html: `
            <p style="
                color:#666;
                font-size:15px;
                margin-top:10px;
                line-height:1.6;">

                Te enviamos un enlace para confirmar tu cuenta.
            </p>
            `,
            confirmButtonText: 'Entendido',
            confirmButtonColor: '#5c3317',
            background: '#f8f5e9',
            color: '#2d2d2d'
        });
        HTMLFormElement.prototype.submit.call(form);
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

        element.style.color = "white";

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