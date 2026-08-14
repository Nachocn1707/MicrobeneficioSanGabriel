(() => {
    "use strict";

    const mensajes = () => {
        const nodo = document.getElementById("sgSystemMessages");
        if (!nodo) return [];
        try { return JSON.parse(nodo.textContent || "[]"); } catch { return []; }
    };

    const escapeHtml = valor => String(valor ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");

    const mostrarToastBootstrap = mensaje => {
        let contenedor = document.getElementById("sgFallbackToasts");
        if (!contenedor) {
            contenedor = document.createElement("div");
            contenedor.id = "sgFallbackToasts";
            contenedor.className = "toast-container position-fixed bottom-0 end-0 p-3";
            contenedor.style.zIndex = "20000";
            document.body.appendChild(contenedor);
        }

        const tipo = ["success", "danger", "warning", "info"].includes(mensaje.tipo)
            ? mensaje.tipo
            : (mensaje.tipo === "error" ? "danger" : "info");
        const toast = document.createElement("div");
        toast.className = "toast show border-0 shadow-lg mb-2";
        toast.setAttribute("role", "status");
        toast.innerHTML = `
            <div class="toast-header border-0 bg-${tipo}-subtle">
                <strong class="me-auto">${escapeHtml(mensaje.titulo || "Información")}</strong>
                <button type="button" class="btn-close" aria-label="Cerrar"></button>
            </div>
            <div class="toast-body bg-white">${escapeHtml(mensaje.texto || "")}</div>`;
        toast.querySelector(".btn-close")?.addEventListener("click", () => toast.remove());
        contenedor.appendChild(toast);
        window.setTimeout(() => toast.remove(), mensaje.tipo === "error" ? 6500 : 4500);
    };

    const mostrarMensajes = () => {
        mensajes().forEach((mensaje, indice) => {
            window.setTimeout(() => {
                if (typeof Swal !== "undefined") {
                    Swal.fire({
                        toast: true,
                        position: "bottom-end",
                        icon: mensaje.tipo || "info",
                        title: mensaje.titulo || "Información",
                        text: mensaje.texto || "",
                        showConfirmButton: false,
                        timer: mensaje.tipo === "error" ? 6000 : 4200,
                        timerProgressBar: true,
                        didOpen: toast => {
                            toast.addEventListener("mouseenter", Swal.stopTimer);
                            toast.addEventListener("mouseleave", Swal.resumeTimer);
                        }
                    });
                } else {
                    mostrarToastBootstrap(mensaje);
                }
            }, indice * 250);
        });
    };

    const enviarEliminacion = href => {
        const token = document.querySelector('#sgGlobalAntiforgery input[name="__RequestVerificationToken"]')?.value;
        if (!token) {
            window.location.href = href;
            return;
        }

        const form = document.createElement("form");
        form.method = "post";
        form.action = href;
        form.style.display = "none";

        const input = document.createElement("input");
        input.type = "hidden";
        input.name = "__RequestVerificationToken";
        input.value = token;
        form.appendChild(input);

        document.body.appendChild(form);
        form.submit();
    };

    /**
     * Reemplaza las alertas genéricas emergentes por un modal interactivo Bootstrap
     * diseñado con la paleta de colores del sitio.
     */
    const mostrarModalConfirmacion = (options = {}) => {
        return new Promise((resolve) => {
            const titulo = options.titulo || "¿Confirmar eliminación?";
            const texto = options.texto || "Esta acción puede afectar información relacionada y no se puede deshacer.";
            const botonConfirmar = options.botonConfirmar || "Sí, eliminar";
            const botonCancelar = options.botonCancelar || "Cancelar";
            const iconoClass = options.iconoClass || "fa-triangle-exclamation";

            let modalEl = document.getElementById("sgGlobalConfirmModal");
            if (!modalEl) {
                modalEl = document.createElement("div");
                modalEl.id = "sgGlobalConfirmModal";
                modalEl.className = "modal fade";
                modalEl.setAttribute("tabindex", "-1");
                modalEl.setAttribute("aria-hidden", "true");
                modalEl.style.zIndex = "10550";
                modalEl.innerHTML = `
                    <div class="modal-dialog modal-dialog-centered" style="max-width: 440px;">
                        <div class="modal-content border-0 shadow-lg" style="border-radius: 24px; overflow: hidden; background: #faf9f5; border: 1px solid rgba(41, 75, 42, 0.14);">
                            <div class="modal-header border-0 pb-0 pt-4 px-4 justify-content-center text-center">
                                <div class="sg-modal-icon-circle" style="width: 68px; height: 68px; border-radius: 50%; background: rgba(142, 60, 44, 0.1); color: #8E3C2C; display: flex; align-items: center; justify-content: center; font-size: 2rem; box-shadow: 0 4px 14px rgba(142, 60, 44, 0.12);">
                                    <i class="fa-solid ${escapeHtml(iconoClass)}" id="sgConfirmModalIcon"></i>
                                </div>
                            </div>
                            <div class="modal-body text-center p-4">
                                <h4 class="fw-bold mb-2" id="sgConfirmModalTitle" style="color: #294b2a; font-family: 'Inter', system-ui, sans-serif; font-size: 1.35rem;"></h4>
                                <p class="text-muted mb-0 fs-6" id="sgConfirmModalBody" style="color: #526053; line-height: 1.5; font-size: 0.95rem;"></p>
                            </div>
                            <div class="modal-footer border-0 p-4 pt-0 gap-2 justify-content-center">
                                <button type="button" class="btn px-4 py-2-5 rounded-pill fw-bold" id="sgConfirmModalBtnCancel" style="background: #e5e0d6; color: #4F3514; border: none; min-width: 120px; font-size: 0.95rem; transition: all 0.2s ease;">
                                    ${escapeHtml(botonCancelar)}
                                </button>
                                <button type="button" class="btn px-4 py-2-5 rounded-pill fw-bold text-white" id="sgConfirmModalBtnConfirm" style="background: linear-gradient(135deg, #a83827, #8E3C2C); border: none; min-width: 130px; font-size: 0.95rem; box-shadow: 0 4px 14px rgba(142, 60, 44, 0.35); transition: all 0.2s ease;">
                                    ${escapeHtml(botonConfirmar)}
                                </button>
                            </div>
                        </div>
                    </div>
                `;
                document.body.appendChild(modalEl);
            }

            document.getElementById("sgConfirmModalTitle").textContent = titulo;
            document.getElementById("sgConfirmModalBody").textContent = texto;
            const iconEl = document.getElementById("sgConfirmModalIcon");
            if (iconEl) iconEl.className = `fa-solid ${iconoClass}`;

            const btnConfirm = document.getElementById("sgConfirmModalBtnConfirm");
            const btnCancel = document.getElementById("sgConfirmModalBtnCancel");

            btnConfirm.textContent = botonConfirmar;
            btnCancel.textContent = botonCancelar;

            let bsModal = null;
            if (window.bootstrap && window.bootstrap.Modal) {
                bsModal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl, { backdrop: 'static', keyboard: true });
            }

            const cleanup = () => {
                btnConfirm.onclick = null;
                btnCancel.onclick = null;
            };

            const confirmHandler = () => {
                cleanup();
                if (bsModal) bsModal.hide();
                resolve(true);
            };

            const cancelHandler = () => {
                cleanup();
                if (bsModal) bsModal.hide();
                resolve(false);
            };

            btnConfirm.onclick = confirmHandler;
            btnCancel.onclick = cancelHandler;

            if (bsModal) {
                bsModal.show();
            } else if (typeof $ !== "undefined" && $.fn && $.fn.modal) {
                $(modalEl).modal('show');
            } else {
                resolve(window.confirm(`${titulo}\n\n${texto}`));
            }
        });
    };

    const confirmarEliminaciones = () => {
        document.addEventListener("click", async event => {
            const enlace = event.target.closest('a[href*="/Delete/"], a[href*="/delete/"], a[data-confirm-delete="true"], button[data-confirm-delete="true"]');
            if (!enlace || enlace.dataset.skipGlobalDelete === "true") return;

            const href = enlace.getAttribute("href");
            if (!href || href === "#") return;

            event.preventDefault();

            const confirmado = await mostrarModalConfirmacion({
                titulo: "¿Confirmar eliminación?",
                texto: "Esta acción puede afectar información relacionada y no se puede deshacer.",
                botonConfirmar: "Sí, eliminar",
                botonCancelar: "Cancelar",
                iconoClass: "fa-triangle-exclamation"
            });

            if (confirmado) {
                enviarEliminacion(href);
            }
        });

        // Intercepta los formularios de confirmación en las vistas Delete.cshtml
        document.addEventListener("submit", async event => {
            const form = event.target;
            if (form && form.getAttribute("action")?.toLowerCase().includes("delete") && !form.dataset.confirmedSubmit) {
                event.preventDefault();
                const confirmado = await mostrarModalConfirmacion({
                    titulo: "¿Confirmar eliminación permanente?",
                    texto: "Esta acción eliminará el registro del sistema de forma definitiva y no se podrá deshacer.",
                    botonConfirmar: "Sí, eliminar",
                    botonCancelar: "Cancelar",
                    iconoClass: "fa-trash-can"
                });
                if (confirmado) {
                    form.dataset.confirmedSubmit = "true";
                    form.submit();
                }
            }
        });
    };

    document.addEventListener("DOMContentLoaded", () => {
        mostrarMensajes();
        confirmarEliminaciones();
    });
})();
