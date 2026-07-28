(() => {
    "use strict";

    document.addEventListener("DOMContentLoaded", () => {
        const form = document.getElementById("createOrderForm");
        if (!form) return;

        const submitButton = form.querySelector('button[type="submit"]');
        const originalButtonHtml = submitButton?.innerHTML || "";
        const summary = form.querySelector('[data-valmsg-summary="true"], .validation-summary-errors, [asp-validation-summary]');

        function clearServerErrors() {
            form.querySelectorAll("[data-valmsg-for]").forEach(element => {
                element.textContent = "";
                element.classList.remove("field-validation-error");
                element.classList.add("field-validation-valid");
            });

            if (summary) {
                summary.innerHTML = "";
                summary.classList.remove("validation-summary-errors");
                summary.classList.add("validation-summary-valid");
            }
        }

        function showValidationErrors(errors, message) {
            let firstInvalidField = null;
            const generalMessages = [];

            Object.entries(errors || {}).forEach(([fieldName, messages]) => {
                const validationElement = form.querySelector(`[data-valmsg-for="${CSS.escape(fieldName)}"]`);
                const text = Array.isArray(messages) ? messages.join(" ") : String(messages || "");

                if (validationElement) {
                    validationElement.textContent = text;
                    validationElement.classList.remove("field-validation-valid");
                    validationElement.classList.add("field-validation-error");
                } else if (text) {
                    generalMessages.push(text);
                }

                if (!firstInvalidField && fieldName) {
                    firstInvalidField = form.querySelector(`[name="${CSS.escape(fieldName)}"]`);
                }
            });

            if (summary && (generalMessages.length > 0 || message)) {
                const list = document.createElement("ul");
                [...generalMessages, message].filter(Boolean).forEach(item => {
                    const li = document.createElement("li");
                    li.textContent = item;
                    list.appendChild(li);
                });
                summary.innerHTML = "";
                summary.appendChild(list);
                summary.classList.remove("validation-summary-valid");
                summary.classList.add("validation-summary-errors");
            }

            firstInvalidField?.focus();
        }

        function clientValidationIsValid() {
            if (window.jQuery && typeof window.jQuery(form).valid === "function") {
                return window.jQuery(form).valid();
            }

            return form.reportValidity();
        }

        form.addEventListener("submit", async event => {
            event.preventDefault();

            if (!clientValidationIsValid()) return;
            clearServerErrors();

            if (!window.SanGabrielPaymentModal?.open) {
                // Respaldo sin JavaScript del popup: conserva el flujo tradicional.
                form.submit();
                return;
            }

            if (submitButton) {
                submitButton.disabled = true;
                submitButton.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Preparando pago...';
            }

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
                    if (result?.errors) {
                        showValidationErrors(result.errors, result.message);
                        return;
                    }

                    throw new Error(result?.message || "No fue posible preparar el pedido para el pago.");
                }

                window.SanGabrielPaymentModal.open({
                    pedidoId: result.pedidoId,
                    pedidoCode: result.codigoPedido,
                    product: result.producto,
                    quantity: result.cantidad,
                    date: result.fecha,
                    subtotalDisplay: result.subtotalDisplay,
                    ivaDisplay: result.ivaDisplay,
                    totalDisplay: result.totalDisplay,
                    redirectUrl: result.redirectUrl
                });
            } catch (error) {
                showValidationErrors({}, error instanceof Error
                    ? error.message
                    : "Ocurrió un error inesperado al preparar el pago.");
            } finally {
                if (submitButton) {
                    submitButton.disabled = false;
                    submitButton.innerHTML = originalButtonHtml;
                }
            }
        });
    });
})();
