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