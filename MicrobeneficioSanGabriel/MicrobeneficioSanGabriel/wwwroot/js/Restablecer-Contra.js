document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".sgl-toggle-visibility").forEach(function (btn) {
        btn.addEventListener("click", function () {
            const input = document.getElementById(btn.getAttribute("data-target"));
            const icon = btn.querySelector("i");
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
        });
    });
});

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

function sglValidateResetForm() {
    const email = document.getElementById("email")?.value.trim() ?? "";
    const password = document.getElementById("password")?.value ?? "";
    const confirmPassword = document.getElementById("confirmPassword")?.value ?? "";

    const emailIsValid = /^[^\s@@]+@@[^\s@@]+\.[^\s@@]+$/.test(email);
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
        if (el) el.addEventListener("input", sglValidateResetForm);
    });
});