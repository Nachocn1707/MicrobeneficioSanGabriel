(() => {
    "use strict";

    document.addEventListener("DOMContentLoaded", () => {
        const modalElement = document.getElementById("paymentModal");
        if (!modalElement || typeof bootstrap === "undefined") return;

        // El popup debe vivir directamente dentro del body para evitar que el
        // fondo oscuro quede por encima cuando el contenido usa capas propias.
        if (modalElement.parentElement !== document.body) {
            document.body.appendChild(modalElement);
        }

        const modal = bootstrap.Modal.getOrCreateInstance(modalElement, {
            backdrop: true,
            keyboard: true,
            focus: true
        });

        const form = document.getElementById("modalPaymentForm");
        const formView = document.getElementById("paymentFormView");
        const successView = document.getElementById("paymentSuccessView");
        const feedback = document.getElementById("paymentModalFeedback");
        const submitButton = document.getElementById("paymentSubmitButton");
        const submitButtonText = document.getElementById("paymentSubmitText");
        const successCloseButton = document.getElementById("paymentSuccessCloseButton");
        const sinpeDetails = document.getElementById("modalSinpeDetails");
        const cashDetails = document.getElementById("modalCashDetails");
        const whatsappButton = document.getElementById("paymentWhatsappButton");
        const successMethod = document.getElementById("paymentSuccessMethod");
        const successOrder = document.getElementById("paymentSuccessOrder");
        const successStatus = document.getElementById("paymentSuccessStatus");
        const successMessage = document.getElementById("paymentSuccessMessage");
        const successTotal = document.getElementById("paymentSuccessTotal");
        const successSinpeNumber = document.getElementById("paymentSuccessSinpeNumber");
        const copyButton = document.getElementById("copySinpeNumber");

        const fields = {
            pedidoId: document.getElementById("modalPedidoId"),
            orderCode: document.getElementById("modalOrderCode"),
            product: document.getElementById("modalProduct"),
            quantity: document.getElementById("modalQuantity"),
            date: document.getElementById("modalDate"),
            subtotal: document.getElementById("modalSubtotal"),
            iva: document.getElementById("modalIva"),
            total: document.getElementById("modalTotal"),
            reference: document.getElementById("modalReference")
        };

        let activePayButton = null;
        let activeTotalDisplay = "";
        let redirectOnClose = "";
        let paymentCompleted = false;

        function selectedMethod() {
            return form?.querySelector('input[name="metodoPago"]:checked')?.value || "";
        }

        function showFeedback(message) {
            if (!feedback) return;
            const text = feedback.querySelector("span");
            if (text) text.textContent = message;
            feedback.classList.add("show");
        }

        function clearFeedback() {
            feedback?.classList.remove("show");
        }

        function updateMethodDetails() {
            const method = selectedMethod();
            if (sinpeDetails) sinpeDetails.hidden = method !== "SINPE Móvil";
            if (cashDetails) cashDetails.hidden = method !== "Efectivo";
        }

        function resetModalState() {
            clearFeedback();
            form?.reset();

            const sinpeRadio = form?.querySelector('input[value="SINPE Móvil"]');
            if (sinpeRadio) sinpeRadio.checked = true;
            updateMethodDetails();

            if (formView) formView.hidden = false;
            if (successView) successView.hidden = true;
            if (submitButton) submitButton.disabled = false;
            if (submitButtonText) submitButtonText.textContent = "Confirmar método de pago";
            if (successCloseButton) successCloseButton.textContent = "Entendido";
        }

        function normaliseDetails(details = {}) {
            return {
                pedidoId: details.pedidoId ?? "",
                pedidoCode: details.pedidoCode || details.codigoPedido || "Pedido nuevo",
                product: details.product || details.producto || "Producto",
                quantity: details.quantity || details.cantidad || "0",
                date: details.date || details.fecha || "—",
                subtotalDisplay: details.subtotalDisplay || "₡0",
                ivaDisplay: details.ivaDisplay || "₡0",
                totalDisplay: details.totalDisplay || "₡0",
                redirectUrl: details.redirectUrl || ""
            };
        }

        function openPaymentModal(details, trigger = null) {
            const data = normaliseDetails(details);

            activePayButton = trigger instanceof HTMLElement ? trigger : null;
            activeTotalDisplay = data.totalDisplay;
            redirectOnClose = data.redirectUrl;
            paymentCompleted = false;

            resetModalState();

            if (fields.pedidoId) fields.pedidoId.value = data.pedidoId;
            if (fields.orderCode) fields.orderCode.textContent = data.pedidoCode;
            if (fields.product) fields.product.textContent = data.product;
            if (fields.quantity) fields.quantity.textContent = `${data.quantity} kg`;
            if (fields.date) fields.date.textContent = data.date;
            if (fields.subtotal) fields.subtotal.textContent = data.subtotalDisplay;
            if (fields.iva) fields.iva.textContent = data.ivaDisplay;
            if (fields.total) fields.total.textContent = data.totalDisplay;
            if (fields.reference) fields.reference.textContent = data.pedidoCode;

            modal.show();
        }

        window.SanGabrielPaymentModal = {
            open: openPaymentModal,
            close: () => modal.hide()
        };

        form?.querySelectorAll('input[name="metodoPago"]').forEach(input => {
            input.addEventListener("change", updateMethodDetails);
        });

        copyButton?.addEventListener("click", async () => {
            const number = copyButton.dataset.number || "89546434";
            try {
                await navigator.clipboard.writeText(number);
                const icon = copyButton.querySelector("i");
                if (icon) icon.className = "fa-solid fa-check";
                copyButton.setAttribute("aria-label", "Número copiado");
                window.setTimeout(() => {
                    if (icon) icon.className = "fa-regular fa-copy";
                    copyButton.setAttribute("aria-label", "Copiar número SINPE");
                }, 1600);
            } catch {
                const temporary = document.createElement("textarea");
                temporary.value = number;
                temporary.style.position = "fixed";
                temporary.style.opacity = "0";
                document.body.appendChild(temporary);
                temporary.select();
                document.execCommand("copy");
                temporary.remove();
            }
        });

        form?.addEventListener("submit", async event => {
            event.preventDefault();
            clearFeedback();

            const method = selectedMethod();
            if (!method) {
                showFeedback("Seleccioná un método de pago para continuar.");
                return;
            }

            if (submitButton) submitButton.disabled = true;
            if (submitButtonText) submitButtonText.textContent = "Procesando...";

            try {
                const response = await fetch(form.action, {
                    method: "POST",
                    body: new FormData(form),
                    credentials: "same-origin",
                    headers: {
                        "X-Requested-With": "XMLHttpRequest",
                        "Accept": "application/json"
                    }
                });

                const contentType = response.headers.get("content-type") || "";
                const result = contentType.includes("application/json")
                    ? await response.json()
                    : null;

                if (!response.ok || !result?.success) {
                    throw new Error(result?.message || "No fue posible registrar el método de pago. Intentá nuevamente.");
                }

                paymentCompleted = true;
                redirectOnClose = result.redirectUrl || redirectOnClose;

                if (formView) formView.hidden = true;
                if (successView) successView.hidden = false;

                if (successMethod) successMethod.textContent = result.metodoPago || method;
                if (successOrder) successOrder.textContent = result.codigoPedido || fields.orderCode?.textContent || "Pedido";
                if (successStatus) successStatus.textContent = result.estadoPago || "Pendiente de pago";
                if (successTotal) successTotal.textContent = activeTotalDisplay;
                if (successSinpeNumber) successSinpeNumber.textContent = result.sinpeNumero || "89546434";
                if (successCloseButton) {
                    successCloseButton.textContent = redirectOnClose ? "Ver mis pedidos" : "Entendido";
                }

                const isSinpe = (result.metodoPago || method) === "SINPE Móvil";
                if (successMessage) {
                    successMessage.textContent = isSinpe
                        ? "El método fue registrado. Realizá el SINPE por el monto indicado y enviá el comprobante para validar el pago."
                        : "El método fue registrado. El pago se realizará en efectivo al retirar el pedido en el establecimiento.";
                }

                if (whatsappButton) {
                    whatsappButton.hidden = !isSinpe;
                    if (isSinpe && result.whatsappUrl) whatsappButton.href = result.whatsappUrl;
                }

                const successSinpeBlock = document.getElementById("paymentSuccessSinpeBlock");
                if (successSinpeBlock) successSinpeBlock.hidden = !isSinpe;

                // En el listado se mantiene el botón mientras el pago siga pendiente,
                // tal como solicita el flujo de pedidos del cliente.
                const card = activePayButton?.closest(".client-purchase-card");
                const badge = card?.querySelector(".client-payment-pill");
                if (badge) {
                    badge.classList.remove("paid");
                    badge.classList.add("pending");
                    badge.innerHTML = '<i class="fa-solid fa-wallet"></i> Pendiente de pago';
                }

                if (activePayButton && result.pedidoId) {
                    activePayButton.dataset.pedidoId = String(result.pedidoId);
                    activePayButton.dataset.pedidoCode = result.codigoPedido || activePayButton.dataset.pedidoCode || "Pedido";
                }
            } catch (error) {
                showFeedback(error instanceof Error ? error.message : "Ocurrió un error inesperado.");
            } finally {
                if (submitButton) submitButton.disabled = false;
                if (submitButtonText) submitButtonText.textContent = "Confirmar método de pago";
            }
        });

        successCloseButton?.addEventListener("click", () => modal.hide());

        modalElement.addEventListener("hidden.bs.modal", () => {
            const nextUrl = paymentCompleted ? redirectOnClose : "";

            activePayButton = null;
            activeTotalDisplay = "";
            redirectOnClose = "";
            paymentCompleted = false;
            resetModalState();

            if (nextUrl) {
                window.location.assign(nextUrl);
            }
        });

        document.querySelectorAll(".js-open-payment-modal").forEach(button => {
            button.addEventListener("click", () => {
                openPaymentModal({
                    pedidoId: button.dataset.pedidoId,
                    pedidoCode: button.dataset.pedidoCode,
                    product: button.dataset.product,
                    quantity: button.dataset.quantity,
                    date: button.dataset.date,
                    subtotalDisplay: button.dataset.subtotalDisplay,
                    ivaDisplay: button.dataset.ivaDisplay,
                    totalDisplay: button.dataset.totalDisplay
                }, button);
            });
        });
    });
})();
