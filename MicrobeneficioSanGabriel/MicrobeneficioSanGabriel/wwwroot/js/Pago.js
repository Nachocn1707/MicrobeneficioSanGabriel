document.addEventListener("DOMContentLoaded", function () {
    const sinpeRadio = document.querySelector('input[value="SINPE Móvil"]');
    const efectivoRadio = document.querySelector('input[value="Efectivo"]');
    const sinpeSection = document.getElementById("sinpeSection");
    const cashSection = document.getElementById("cashSection");
    const nombreCompleto = document.getElementById("nombreCompleto");
    const provincia = document.getElementById("provincia");
    const form = document.getElementById("paymentForm");

    function actualizarMetodoPago() {
        const esSinpe = sinpeRadio?.checked === true;

        if (sinpeSection) {
            sinpeSection.style.display = esSinpe ? "block" : "none";
        }

        if (cashSection) {
            cashSection.style.display = esSinpe ? "none" : "block";
        }
    }

    sinpeRadio?.addEventListener("change", actualizarMetodoPago);
    efectivoRadio?.addEventListener("change", actualizarMetodoPago);
    actualizarMetodoPago();

    nombreCompleto?.addEventListener("input", function () {
        this.value = this.value.replace(/[^a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]/g, "");
    });

    form?.addEventListener("submit", function (e) {
        const metodoSeleccionado = document.querySelector('input[name="metodoPago"]:checked');

        if (!metodoSeleccionado) {
            Swal.fire({
                icon: "warning",
                title: "Método requerido",
                text: "Debe seleccionar una forma de pago."
            });
            e.preventDefault();
            return;
        }

        if (!nombreCompleto || nombreCompleto.value.trim() === "") {
            Swal.fire({
                icon: "warning",
                title: "Campo requerido",
                text: "Debe ingresar el nombre completo."
            });
            e.preventDefault();
            return;
        }

        if (!provincia || provincia.value === "") {
            Swal.fire({
                icon: "warning",
                title: "Provincia requerida",
                text: "Debe seleccionar la provincia."
            });
            e.preventDefault();
            return;
        }

        const metodo = metodoSeleccionado.value;
        const esPedidoPendiente = form?.dataset.pendingOrder === "true";
        const title = document.getElementById("loadingTitle");
        const message = document.getElementById("loadingMessage");
        const overlay = document.getElementById("loadingOverlay");
        const submitButton = document.querySelector(".btn-pay");

        if (metodo === "SINPE Móvil") {
            title.textContent = esPedidoPendiente
                ? "Creando pedido y registrando SINPE Móvil"
                : "Registrando pago por SINPE Móvil";
            message.textContent = "El comprobante quedará pendiente de confirmación.";
        } else {
            title.textContent = esPedidoPendiente
                ? "Creando pedido con pago en efectivo"
                : "Registrando pago en efectivo";
            message.textContent = "Preparando la solicitud del pedido...";
        }

        if (overlay) {
            overlay.style.display = "flex";
        }

        if (submitButton) {
            submitButton.disabled = true;
        }
    });
});
