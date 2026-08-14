(() => {
    "use strict";

    const tableApis = new Map();

    const spanishLanguage = {
        emptyTable: "No hay datos disponibles.",
        info: "Mostrando _START_ a _END_ de _TOTAL_ registros",
        infoEmpty: "Mostrando 0 registros",
        infoFiltered: "(filtrado de _MAX_ registros)",
        lengthMenu: "Mostrar _MENU_ registros",
        loadingRecords: "Cargando...",
        processing: "Procesando...",
        search: "Buscar:",
        zeroRecords: "No se encontraron resultados",
        paginate: {
            first: "Primero",
            last: "Último",
            next: "Siguiente",
            previous: "Anterior"
        },
        aria: {
            sortAscending: ": activar para ordenar ascendente",
            sortDescending: ": activar para ordenar descendente"
        }
    };

    function hasDataTables() {
        return Boolean(window.jQuery && jQuery.fn && jQuery.fn.DataTable);
    }

    function isDataTable(table) {
        return hasDataTables() && jQuery.fn.DataTable.isDataTable(table);
    }

    function getApi(table) {
        if (!table || !table.id || !hasDataTables()) {
            return null;
        }

        if (tableApis.has(table.id)) {
            return tableApis.get(table.id);
        }

        if (isDataTable(table)) {
            const api = jQuery(table).DataTable();
            tableApis.set(table.id, api);
            return api;
        }

        return null;
    }

    function removePlaceholderRows(table) {
        const headerColumns = table.querySelectorAll("thead th").length;
        if (headerColumns === 0) return;

        table.querySelectorAll("tbody tr").forEach(row => {
            const cells = row.querySelectorAll(":scope > td, :scope > th");
            if (cells.length !== 1) return;

            const colspan = Number(cells[0].getAttribute("colspan") || "1");
            if (colspan >= headerColumns) {
                // DataTables no admite colspan en tbody para una fila de datos.
                // Su mensaje emptyTable reemplaza correctamente esta fila vacía.
                row.remove();
            }
        });
    }

    function initTable(table) {
        if (!table || !table.id || table.dataset.sgDtProcessed === "true") {
            return getApi(table);
        }

        table.dataset.sgDtProcessed = "true";

        if (!hasDataTables()) {
            return null;
        }

        removePlaceholderRows(table);

        try {
            const api = isDataTable(table)
                ? jQuery(table).DataTable()
                : jQuery(table).DataTable({
                    language: spanishLanguage,
                    pageLength: 5,
                    lengthMenu: [5, 10, 15, 20, 50],
                    autoWidth: false,
                    order: [],
                    dom: 'rt<"sg-dt-footer d-flex flex-wrap align-items-center justify-content-between gap-3 mt-3"ip>',
                    columnDefs: [
                        {
                            targets: -1,
                            orderable: false,
                            searchable: false
                        }
                    ]
                });

            tableApis.set(table.id, api);
            return api;
        } catch (error) {
            console.warn("No se pudo inicializar DataTables para", table.id, error);
            return null;
        }
    }

    function normalize(value) {
        return (value || "")
            .toString()
            .toLowerCase()
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .trim();
    }

    function fallbackFilter(table, value) {
        const search = normalize(value);
        const rows = table.querySelectorAll("tbody tr");

        rows.forEach(row => {
            const text = normalize(row.textContent);
            row.style.display = !search || text.includes(search) ? "" : "none";
        });
    }

    function findTargetTable(input) {
        const explicitTarget = input.dataset.tableTarget;

        if (explicitTarget) {
            return document.querySelector(explicitTarget.startsWith("#") ? explicitTarget : `#${explicitTarget}`);
        }

        if (input.id === "sgProductSearch") {
            return document.getElementById("tablaProductos");
        }

        const container = input.closest("main") || document;
        const tables = Array.from(container.querySelectorAll("table.sg-table[id]"));

        if (tables.length === 0) {
            return null;
        }

        if (tables.length === 1) {
            return tables[0];
        }

        const inputTop = input.getBoundingClientRect().top;
        const nextTable = tables.find(table => table.getBoundingClientRect().top >= inputTop);

        return nextTable || tables[0];
    }

    function wireSearchInput(input) {
        if (input.dataset.sgSearchReady === "true") {
            return;
        }

        input.dataset.sgSearchReady = "true";

        input.addEventListener("input", () => {
            const table = findTargetTable(input);

            if (!table) {
                return;
            }

            const api = getApi(table) || initTable(table);

            if (api) {
                api.search(input.value).draw();
            } else {
                fallbackFilter(table, input.value);
            }
        });
    }

    function init() {
        document
            .querySelectorAll("table.sg-table[id]")
            .forEach(initTable);

        document
            .querySelectorAll(".sg-search input")
            .forEach(wireSearchInput);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
