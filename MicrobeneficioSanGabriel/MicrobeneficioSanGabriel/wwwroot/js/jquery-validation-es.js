(function ($) {
    "use strict";

    if (!$ || !$.validator) {
        return;
    }

    $.extend($.validator.messages, {
        required: "Este campo es obligatorio.",
        remote: "Corrija este campo.",
        email: "Ingrese un correo electrónico válido.",
        url: "Ingrese una dirección web válida.",
        date: "Ingrese una fecha válida.",
        dateISO: "Ingrese una fecha válida (AAAA-MM-DD).",
        number: "Ingrese un número válido.",
        digits: "Ingrese únicamente números enteros.",
        equalTo: "Ingrese nuevamente el mismo valor.",
        maxlength: $.validator.format("No ingrese más de {0} caracteres."),
        minlength: $.validator.format("Ingrese al menos {0} caracteres."),
        rangelength: $.validator.format("Ingrese un valor entre {0} y {1} caracteres."),
        range: $.validator.format("Ingrese un valor entre {0} y {1}."),
        max: $.validator.format("Ingrese un valor menor o igual a {0}."),
        min: $.validator.format("Ingrese un valor mayor o igual a {0}."),
        step: $.validator.format("Ingrese un múltiplo de {0}.")
    });
})(window.jQuery);
