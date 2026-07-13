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
    form.addEventListener("submit", async function (e) {
        e.preventDefault();

        if (!form.reportValidity()) {
            return;
        }
        const password = document.getElementById("password");
        const confirmPassword = document.getElementById("confirmPassword");
        if (password && confirmPassword) {
            const value = password.value;
            const validPassword =
                value.length >= 8 &&
                /[A-Z]/.test(value) &&
                /[a-z]/.test(value) &&
                /[0-9]/.test(value) &&
                /[^A-Za-z0-9]/.test(value);
            const match = password.value === confirmPassword.value;
            if (!validPassword || !match) {
                return;
            }
        }
        await Swal.fire({
            html: `
            <div style="padding:10px 5px;">
                <div style="
                    width:85px;
                    height:85px;
                    margin:0 auto 25px;
                    background:#5c3317;
                    border-radius:50%;
                    display:flex;
                    align-items:center;
                    justify-content:center;
                    box-shadow:0 10px 25px rgba(0,0,0,0.15);">
                    <i class="fa-solid fa-envelope"
                       style="
                        color:#f8f5e9;
                        font-size:34px;">
                    </i>
                </div>
                <h2 style="
                    color:#2b1408;
                    font-size:30px;
                    font-weight:800;
                    margin-bottom:18px;">

                    Revisá tu correo
                </h2>
                <p style="
                    color:#666;
                    font-size:15px;
                    line-height:1.8;
                    margin-bottom:30px;">

                    Te enviamos un enlace para confirmar tu cuenta.
                </p>
            </div>
            `,
            confirmButtonText: 'ENTENDIDO',
            confirmButtonColor: '#5c3317',
            background: '#f8f5e9',
            color: '#2d2d2d',
            width: '480px',
            padding: '2.5rem',
            borderRadius: '28px',
            customClass: {
                popup: 'custom-register-popup',
                confirmButton: 'custom-register-button'
            }
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