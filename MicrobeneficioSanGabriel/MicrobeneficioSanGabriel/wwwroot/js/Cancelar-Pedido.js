document.addEventListener("DOMContentLoaded", function () {
    const modalElement = document.getElementById("anularFacturaModal");
    if (!modalElement || typeof bootstrap === "undefined") return;

    if (modalElement.parentElement !== document.body) {
        document.body.appendChild(modalElement);
    }

    const modal = bootstrap.Modal.getOrCreateInstance(modalElement, {
        backdrop: true,
        keyboard: true,
        focus: true
    });

    document.addEventListener("click", function (e) {
        const button = e.target.closest(".js-open-anular-modal");
        if (!button) return;

        e.preventDefault();

        const id = button.getAttribute("data-factura-id") || "";
        const code = button.getAttribute("data-factura-code") || "FAC-0000";
        const cliente = button.getAttribute("data-cliente") || "—";
        const total = button.getAttribute("data-total") || "—";

        const inputId = document.getElementById("anularFacturaId");
        const codeEl = document.getElementById("anularFacturaCode");
        const clienteEl = document.getElementById("anularFacturaCliente");
        const totalEl = document.getElementById("anularFacturaTotal");

        if (inputId) inputId.value = id;
        if (codeEl) codeEl.textContent = code;
        if (clienteEl) clienteEl.textContent = cliente;
        if (totalEl) totalEl.textContent = total;

        modal.show();
    });
});