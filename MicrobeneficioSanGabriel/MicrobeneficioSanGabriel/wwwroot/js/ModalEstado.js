document.addEventListener("DOMContentLoaded", function () {
    const escapeHtml = function (str) {
        return String(str ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    };

    const etapas = ["Lavado", "Secado", "Tostado", "Molido", "Empaque"];

    document.querySelectorAll(".js-confirmar-avance").forEach(function (boton) {
        boton.addEventListener("click", function () {
            const formId = boton.dataset.formId;
            const proceso = boton.dataset.proceso || "la etapa actual";
            const lote = boton.dataset.lote || "";
            const formulario = document.getElementById(formId);

            if (!formulario) {
                return;
            }

            const confirmarAvance = function () {

                formulario.submit();
            };

            const idx = etapas.findIndex(e => e.toLowerCase() === proceso.toLowerCase());
            let siguiente = "Siguiente etapa";
            let esUltimaEtapa = false;

            if (idx >= 0 && idx < etapas.length - 1) {
                siguiente = etapas[idx + 1];
            } else if (idx === etapas.length - 1) {
                siguiente = "Completada";
                esUltimaEtapa = true;
            }

            if (typeof Swal === "undefined") {
                if (window.confirm(`¿Desea marcar ${proceso} como completado y continuar con ${siguiente}?`)) {
                    confirmarAvance();
                }
                return;
            }

            const titulo = esUltimaEtapa ? "¿Completar producción?" : "¿Confirmar avance de etapa?";
            const descTexto = esUltimaEtapa
                ? `Se marcará la fase final de <strong>${escapeHtml(proceso)}</strong> como completada y el lote finalizará su ciclo de producción.`
                : `Se completará la fase de <strong>${escapeHtml(proceso)}</strong> y se iniciará automáticamente la etapa de <strong>${escapeHtml(siguiente)}</strong>.`;
            const btnConfirmTexto = esUltimaEtapa
                ? '<i class="fa-solid fa-flag-checkered me-2"></i>Finalizar producción'
                : `<i class="fa-solid fa-forward-step me-2"></i>Avanzar a ${escapeHtml(siguiente)}`;

            Swal.fire({
                html: `
                            <div class="sg-prod-modal-wrap">
                                <div class="sg-prod-modal-icon-circle">
                                    <i class="fa-solid ${esUltimaEtapa ? 'fa-flag-checkered' : 'fa-forward-step'}"></i>
                                </div>
                                <h3 class="sg-prod-modal-title">${titulo}</h3>
                                
                                <div class="sg-prod-modal-card">
                                    ${lote ? `
                                    <div class="sg-prod-modal-lotetag">
                                        <i class="fa-solid fa-layer-group text-success"></i>
                                        <span>Lote: <strong>${escapeHtml(lote)}</strong></span>
                                    </div>` : ''}
                                    
                                    <div class="sg-prod-modal-flow">
                                        <div class="sg-prod-flow-step done">
                                            <span class="sg-prod-step-badge done">
                                                <i class="fa-solid fa-check"></i> Completando
                                            </span>
                                            <span class="sg-prod-step-name">${escapeHtml(proceso)}</span>
                                        </div>
                                        
                                        <div class="sg-prod-flow-arrow">
                                            <i class="fa-solid fa-arrow-right-long"></i>
                                        </div>
                                        
                                        <div class="sg-prod-flow-step next">
                                            <span class="sg-prod-step-badge next">
                                                <i class="fa-solid ${esUltimaEtapa ? 'fa-circle-check' : 'fa-spinner fa-spin'}"></i> ${esUltimaEtapa ? 'Final' : 'Siguiente'}
                                            </span>
                                            <span class="sg-prod-step-name">${escapeHtml(siguiente)}</span>
                                        </div>
                                    </div>
                                </div>
                                
                                <p class="sg-prod-modal-desc">${descTexto}</p>
                            </div>
                        `,
                showCancelButton: true,
                confirmButtonText: btnConfirmTexto,
                cancelButtonText: '<i class="fa-solid fa-xmark me-2"></i>Cancelar',
                reverseButtons: true,
                focusCancel: true,
                allowOutsideClick: false,
                buttonsStyling: false,
                customClass: {
                    popup: "sg-prod-swal-popup",
                    htmlContainer: "sg-prod-swal-html",
                    actions: "sg-prod-swal-actions",
                    confirmButton: "sg-prod-swal-btn-confirm",
                    cancelButton: "sg-prod-swal-btn-cancel"
                }
            }).then(function (resultado) {
                if (resultado.isConfirmed) {
                    confirmarAvance();
                }
            });
        });
    });
});