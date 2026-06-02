document.addEventListener("DOMContentLoaded", function () {

    const tarjetaRadio = document.querySelector('input[value="Tarjeta"]');
    const efectivoRadio = document.querySelector('input[value="Efectivo"]');
    const cardSection = document.getElementById("cardSection");
    const cashSection = document.getElementById("cashSection");
    const numeroTarjeta = document.getElementById("numeroTarjeta");
    const numeroSecreto = document.getElementById("numeroSecreto");
    const fechaVencimiento = document.getElementById("fechaVencimiento");
    const nombreCompleto = document.getElementById("nombreCompleto");
    const cardLogo = document.getElementById("cardLogo");

    function actualizarMetodoPago() {

        const esTarjeta = tarjetaRadio.checked;

        cardSection.style.display = esTarjeta ? "block" : "none";
        cashSection.style.display = esTarjeta ? "none" : "block";

        numeroTarjeta.required = esTarjeta;
        fechaVencimiento.required = esTarjeta;
        numeroSecreto.required = esTarjeta;
    }

    tarjetaRadio.addEventListener("change",actualizarMetodoPago);
    efectivoRadio.addEventListener("change",actualizarMetodoPago);

    actualizarMetodoPago();

    numeroTarjeta.addEventListener(
        "input",
        function () {let numero = this.value.replace(/\D/g, '');

            numero = numero.substring(0, 16);

            if (numero.startsWith('4')) {
                cardLogo.src ="/img/Tarjetas/Visa.png";
                cardLogo.alt = "Visa";
                cardLogo.style.display ="block";
            }
            else if (numero.startsWith('5')) {
                cardLogo.src = "/img/Tarjetas/MasterCard.png";
                cardLogo.alt = "MasterCard";
                cardLogo.style.display ="block";
            }
            else {
                cardLogo.style.display =
                    "none";
            }
            const formateado =numero.replace(
                    /(\d{4})(?=\d)/g,
                    '$1 '
                );
            this.value = formateado;
        });

    numeroSecreto.addEventListener("input",
        function () {this.value = this.value.replace(/\D/g, '');
        });

    nombreCompleto.addEventListener("input",function () {
            this.value = this.value.replace(/[^a-zA-ZáéíóúÁÉÍÓÚñÑ\s]/g,'');
        });

    fechaVencimiento.addEventListener(
        "input",
        function () {
            let valor =this.value.replace(/\D/g, '');
            valor = valor.substring(0, 4);
            if (valor.length > 2) {
                valor =
                    valor.substring(0, 2)
                    + "/"
                    + valor.substring(2);
            }
            this.value = valor;
        });

    document.querySelector("form").addEventListener("submit", function (e) {
        const metodo = document.querySelector('input[name="metodoPago"]:checked').value;
        if (
            nombreCompleto.value.trim() === ""
        ) {
            Swal.fire({
                icon: 'warning',
                title: 'Campo requerido',
                text: 'Debe ingresar el nombre completo.'
            });

            e.preventDefault();
            return;
        }
        if (metodo === "Tarjeta") {
            const tarjeta = numeroTarjeta.value.replace(/\s/g, '');
            const fecha =fechaVencimiento.value;
            const cvv =numeroSecreto.value;

            if (tarjeta.length !== 16) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Tarjeta inválida',
                    text: 'La tarjeta debe contener 16 dígitos.'
                });
                e.preventDefault();
                return;
            }
            if (fecha.length !== 5) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Fecha inválida',
                    text: 'Debe ingresar la fecha en formato MM/AA.'
                });

                e.preventDefault();
                return;
            }
            if (cvv.length !== 3) {
                Swal.fire({
                    icon: 'warning',
                    title: 'CVV inválido',
                    text: 'El CVV debe contener 3 dígitos.'
                });
                e.preventDefault();
                return;
            }
        }
        const title = document.getElementById("loadingTitle");
        const message = document.getElementById("loadingMessage"
            );

        if (metodo === "Tarjeta") {
            title.textContent = "Procesando pago";
            message.textContent = "Validando datos de la tarjeta...";
        }
        else {
            title.textContent = "Registrando pedido";
            message.textContent = "Preparando su solicitud...";
        }
        document.getElementById("loadingOverlay").style.display = "flex";
        document.querySelector(".btn-pay").disabled = true;

        e.preventDefault();

        const form = this;
        setTimeout(() => {

            if (metodo === "Tarjeta") {
                message.textContent = "Procesando compra...";
            }
        }, 1500);
        setTimeout(() => {
            form.submit();
        }, 3000);
    });
});