document.addEventListener("DOMContentLoaded", function () {

    const toggle = document.getElementById("theme-toggle");

    const isDark = localStorage.getItem("theme") === "dark";

    if (isDark) {
        document.documentElement.classList.add("dark");
        document.body.classList.add("dark");
    } else {
        document.documentElement.classList.remove("dark");
        document.body.classList.remove("dark");
    }

    if (toggle) {
        toggle.checked = isDark;

        toggle.addEventListener("change", function () {

            if (this.checked) {
                document.documentElement.classList.add("dark");
                document.body.classList.add("dark");
                localStorage.setItem("theme", "dark");
            } else {
                document.documentElement.classList.remove("dark");
                document.body.classList.remove("dark");
                localStorage.setItem("theme", "light");
            }

            if (typeof actualizarGraficoTema === "function") {
                actualizarGraficoTema();
            }
        });
    }

});