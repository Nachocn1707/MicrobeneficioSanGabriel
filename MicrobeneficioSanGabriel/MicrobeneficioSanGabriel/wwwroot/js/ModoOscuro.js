(() => {
    "use strict";

    const aplicar = tema => {
        const oscuro = tema === "dark";
        document.body.classList.toggle("dark", oscuro);
        document.documentElement.dataset.theme = oscuro ? "dark" : "light";
    };

    const temaGuardado = localStorage.getItem("theme");
    if (temaGuardado === "dark" || temaGuardado === "light") aplicar(temaGuardado);

    document.addEventListener("click", event => {
        const boton = event.target.closest("[data-theme-toggle]");
        if (!boton) return;
        const nuevo = document.body.classList.contains("dark") ? "light" : "dark";
        localStorage.setItem("theme", nuevo);
        aplicar(nuevo);
        window.dispatchEvent(new CustomEvent("sg-theme-changed", { detail: { theme: nuevo } }));
    });
})();
