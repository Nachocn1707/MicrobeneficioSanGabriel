(() => {
    "use strict";

    document.addEventListener("DOMContentLoaded", () => {
        const modalElement = document.getElementById("cancelModal");
        if (!modalElement || typeof bootstrap === "undefined") return;

        if (modalElement.parentElement !== document.body) {
            document.body.appendChild(modalElement);
        }

        const modal = bootstrap.Modal.getOrCreateInstance(modalElement, {
            backdrop: true,
            keyboard: true,
            focus: true
        });

        document.addEventListener("click", (e) => {
            const button = e.target.closest(".js-open-cancel-modal");
            if (!button) return;

            e.preventDefault();

            const pedidoId = button.getAttribute("data-pedido-id") || "";
            const pedidoCode = button.getAttribute("data-pedido-code") || "PP-0000";
            const product = button.getAttribute("data-product") || "—";
            const quantity = button.getAttribute("data-quantity") || "—";
            const date = button.getAttribute("data-date") || "—";
            const phone = button.getAttribute("data-phone") || "Sin teléfono";
            const obs = button.getAttribute("data-obs") || "Sin observación";

            const inputId = document.getElementById("cancelModalPedidoId");
            const codeEl = document.getElementById("cancelModalOrderCode");
            const prodEl = document.getElementById("cancelModalProduct");
            const qtyEl = document.getElementById("cancelModalQuantity");
            const dateEl = document.getElementById("cancelModalDate");
            const phoneEl = document.getElementById("cancelModalPhone");
            const obsEl = document.getElementById("cancelModalObs");

            if (inputId) inputId.value = pedidoId;
            if (codeEl) codeEl.textContent = pedidoCode;
            if (prodEl) prodEl.textContent = product;
            if (qtyEl) qtyEl.textContent = quantity;
            if (dateEl) dateEl.textContent = date;
            if (phoneEl) phoneEl.textContent = phone;
            if (obsEl) obsEl.textContent = obs;

            modal.show();
        });
    });
})();
