(() => {
    "use strict";

    if (window.jQuery?.validator?.unobtrusive) {
        const $ = window.jQuery;
        const correoCompleto = /^[A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+@[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?(?:\.[A-Za-z]{2,})+$/;

        $.validator.addMethod("notwhitespace", function (value, element) {
            return this.optional(element) || value.trim().length > 0;
        });
        $.validator.unobtrusive.adapters.addBool("notwhitespace");

        $.validator.addMethod("completeemail", function (value, element) {
            return this.optional(element) || correoCompleto.test(value.trim());
        });
        $.validator.unobtrusive.adapters.addBool("completeemail");

        $.validator.addMethod("wholenumber", function (value, element) {
            if (this.optional(element)) {
                return true;
            }

            const normalizado = value.trim().replace(/\s+/g, "").replace(",", ".");
            return /^-?\d+(?:\.0+)?$/.test(normalizado) && Number.isInteger(Number(normalizado));
        });
        $.validator.unobtrusive.adapters.addBool("wholenumber");
    }

    const mostrarError = (campo, mensaje) => {
        const form = campo.form;
        if (!form) {
            return;
        }

        const contenedor = form.querySelector(`[data-valmsg-for="${CSS.escape(campo.name)}"]`);
        if (contenedor) {
            contenedor.textContent = mensaje || "";
            contenedor.classList.toggle("field-validation-error", Boolean(mensaje));
            contenedor.classList.toggle("field-validation-valid", !mensaje);
        }
    };

    const validarEntero = campo => {
        const valor = campo.value.trim();
        if (valor === "") {
            mostrarError(campo, "");
            return true;
        }

        const minimo = Number(campo.dataset.integerMin);
        const maximo = Number(campo.dataset.integerMax);
        const numero = Number(valor);
        const valido = /^\d+$/.test(valor) &&
            Number.isSafeInteger(numero) &&
            numero >= minimo && numero <= maximo;

        mostrarError(campo, valido ? "" : campo.dataset.rangeMessage);
        return valido;
    };

    const validarSinEspacios = campo => {
        const valido = campo.value.trim().length > 0;
        mostrarError(campo, valido ? "" : campo.dataset.whitespaceMessage);
        return valido;
    };

    const validarFechaNoPasada = campo => {
        const fechaSeleccionada = (campo.value || "").slice(0, 10);
        if (!fechaSeleccionada) {
            mostrarError(campo, "");
            return true;
        }

        const esPasada = fechaSeleccionada < campo.dataset.today;
        const esFechaOriginal = fechaSeleccionada === campo.dataset.originalDate;
        const valido = !esPasada || esFechaOriginal;

        mostrarError(campo, valido ? "" : campo.dataset.dateMessage);
        return valido;
    };

    document.addEventListener("DOMContentLoaded", () => {
        const enteros = [...document.querySelectorAll("[data-integer-min][data-integer-max]")];
        const responsables = [...document.querySelectorAll("[data-not-whitespace='true']")];
        const fechas = [...document.querySelectorAll("[data-no-past-date='true']")];

        enteros.forEach(campo => {
            campo.addEventListener("input", () => validarEntero(campo));
            campo.addEventListener("blur", () => validarEntero(campo));
        });

        responsables.forEach(campo => {
            campo.addEventListener("input", () => validarSinEspacios(campo));
            campo.addEventListener("blur", () => validarSinEspacios(campo));
        });

        fechas.forEach(campo => {
            campo.addEventListener("input", () => validarFechaNoPasada(campo));
            campo.addEventListener("change", () => validarFechaNoPasada(campo));
        });

        const formularios = new Set([
            ...enteros.map(campo => campo.form),
            ...responsables.map(campo => campo.form),
            ...fechas.map(campo => campo.form)
        ].filter(Boolean));

        formularios.forEach(form => {
            form.addEventListener("submit", event => {
                const camposInvalidos = [
                    ...enteros.filter(campo => campo.form === form && !validarEntero(campo)),
                    ...responsables.filter(campo => campo.form === form && !validarSinEspacios(campo)),
                    ...fechas.filter(campo => campo.form === form && !validarFechaNoPasada(campo))
                ];

                if (camposInvalidos.length > 0) {
                    event.preventDefault();
                    camposInvalidos[0].focus();
                }
            }, true);
        });
    });
})();
