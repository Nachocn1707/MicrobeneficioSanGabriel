(() => {
    "use strict";

    const root = document.getElementById("notificationRoot");

    if (!root) {
        return;
    }

    const button = document.getElementById("notificationButton");
    const panel = document.getElementById("notificationPanel");
    const count = document.getElementById("notificationCount");
    const panelCount = document.getElementById("notificationPanelCount");
    const empty = document.getElementById("notificationEmpty");
    const bellIcon = document.getElementById("notificationBellIcon");
    const toggle = document.getElementById("notificationsToggle");
    const statusText = document.getElementById("notificationStatusText");
    const dataElement = document.getElementById("notificationData");

    const userId = root.dataset.userId || "anonimo";
    const canManage = root.dataset.canManage === "true";

    const dismissedKey =
        `san-gabriel-alertas-eliminadas-${userId}`;

    const shownKey =
        `san-gabriel-alertas-mostradas-${userId}`;

    const enabledKey =
        `san-gabriel-notificaciones-activas-${userId}`;

    let alertas = [];

    try {
        alertas = JSON.parse(dataElement?.textContent || "[]");
    } catch {
        alertas = [];
    }

    let toastArea = document.getElementById("alertToastArea");

    if (!toastArea) {
        toastArea = document.createElement("div");
        toastArea.id = "alertToastArea";
        toastArea.className = "demo-toast-area";
        document.body.appendChild(toastArea);
    }

    function readArray(key) {
        try {
            return JSON.parse(localStorage.getItem(key)) || [];
        } catch {
            return [];
        }
    }

    function writeArray(key, value) {
        localStorage.setItem(key, JSON.stringify(value));
    }

    function notificationsEnabled() {
        if (!canManage) {
            return true;
        }

        return localStorage.getItem(enabledKey) !== "false";
    }

    function updateToggle() {
        const enabled = notificationsEnabled();

        if (toggle) {
            toggle.checked = enabled;
        }

        if (statusText) {
            statusText.textContent = enabled
                ? "Activadas"
                : "Desactivadas";
        }

        if (button) {
            button.classList.toggle(
                "notifications-disabled",
                !enabled
            );

            button.title = enabled
                ? "Notificaciones activadas"
                : "Notificaciones desactivadas";
        }

        if (bellIcon) {
            bellIcon.className = enabled
                ? "fa-regular fa-bell"
                : "fa-solid fa-bell-slash";
        }
    }

    function refresh() {
        const enabled = notificationsEnabled();
        const dismissed = readArray(dismissedKey);
        let visible = 0;

        root.querySelectorAll("[data-alert-key]")
            .forEach(item => {
                const key = item.dataset.alertKey;
                const show = enabled && !dismissed.includes(key);

                item.style.display = show ? "block" : "none";

                if (show) {
                    visible++;
                }
            });

        if (count) {
            count.textContent = visible;
            count.style.display =
                enabled && visible > 0
                    ? "inline-flex"
                    : "none";
        }

        if (panelCount) {
            panelCount.textContent = enabled
                ? `${visible} alerta(s)`
                : "Notificaciones desactivadas";
        }

        if (empty) {
            empty.textContent = enabled
                ? "No hay alertas pendientes."
                : "Las notificaciones están desactivadas.";

            empty.style.display =
                !enabled || visible === 0
                    ? "block"
                    : "none";
        }

        updateToggle();
    }

    function dismissAlert(key) {
        const dismissed = readArray(dismissedKey);

        if (!dismissed.includes(key)) {
            dismissed.push(key);
            writeArray(dismissedKey, dismissed);
        }

        refresh();
    }

    root.querySelectorAll("[data-alert-dismiss]")
        .forEach(closeButton => {
            closeButton.addEventListener("click", event => {
                event.preventDefault();
                event.stopPropagation();

                dismissAlert(closeButton.dataset.alertDismiss);
            });
        });

    if (button && panel) {
        button.addEventListener("click", event => {
            event.stopPropagation();
            panel.hidden = !panel.hidden;
        });

        document.addEventListener("click", event => {
            if (!root.contains(event.target)) {
                panel.hidden = true;
            }
        });

        document.addEventListener("keydown", event => {
            if (event.key === "Escape") {
                panel.hidden = true;
            }
        });
    }

    function showToast(alerta) {
        if (!notificationsEnabled()) {
            return;
        }

        const dismissed = readArray(dismissedKey);
        const shown = readArray(shownKey);

        if (
            dismissed.includes(alerta.clave) ||
            shown.includes(alerta.clave)
        ) {
            return;
        }

        const validTypes = [
            "success",
            "warning",
            "danger",
            "info"
        ];

        const type = validTypes.includes(alerta.tipo)
            ? alerta.tipo
            : "info";

        const toast = document.createElement("article");
        toast.className = `demo-toast demo-toast-${type}`;

        const closeButton = document.createElement("button");
        closeButton.type = "button";
        closeButton.setAttribute("aria-label", "Cerrar");
        closeButton.innerHTML =
            '<i class="fa-solid fa-xmark"></i>';

        const title = document.createElement("strong");
        title.textContent = alerta.titulo;

        const message = document.createElement("span");
        message.textContent = alerta.mensaje;

        const time = document.createElement("small");
        time.textContent = alerta.tiempo;

        closeButton.addEventListener("click", () => {
            dismissAlert(alerta.clave);
            toast.remove();
        });

        toast.append(closeButton, title, message, time);
        toastArea.appendChild(toast);

        shown.push(alerta.clave);
        writeArray(shownKey, shown);

        setTimeout(() => {
            toast.remove();
        }, 6000);
    }

    if (toggle) {
        toggle.addEventListener("change", () => {
            localStorage.setItem(
                enabledKey,
                toggle.checked ? "true" : "false"
            );

            if (!toggle.checked) {
                toastArea.replaceChildren();
            }

            refresh();

            if (toggle.checked) {
                alertas.slice(0, 3).forEach(showToast);
            }
        });
    }

    alertas.slice(0, 3).forEach(showToast);
    refresh();
})();