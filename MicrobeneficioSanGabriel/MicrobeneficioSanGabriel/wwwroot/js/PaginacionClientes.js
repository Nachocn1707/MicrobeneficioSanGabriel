document.addEventListener('DOMContentLoaded', function () {
    const filterBar = document.getElementById('clientOrdersFilterBar');
    const ordersList = document.querySelector('.client-orders-list');
    if (!filterBar || !ordersList) return;

    const cards = Array.from(ordersList.querySelectorAll('.client-purchase-card'));
    const searchInput = document.getElementById('clientOrderSearchInput');
    const clearSearchBtn = document.getElementById('clientClearSearchBtn');
    const statusSelect = document.getElementById('clientOrderFilterStatus');
    const paymentSelect = document.getElementById('clientOrderFilterPayment');
    const pageSizeSelect = document.getElementById('clientOrderPageSize');
    const paginationBar = document.getElementById('clientOrdersPaginationBar');
    const paginationInfo = document.getElementById('clientPaginationInfo');
    const paginationControls = document.getElementById('clientPaginationControls');
    const noResultsDiv = document.getElementById('clientOrdersNoResults');
    const resetFiltersBtn = document.getElementById('clientResetFiltersBtn');

    let currentPage = 1;

    function applyFiltersAndPaginate() {
        const query = (searchInput ? searchInput.value : '').trim().toLowerCase();
        const selectedStatus = (statusSelect ? statusSelect.value : '').trim();
        const selectedPayment = (paymentSelect ? paymentSelect.value : '').trim();
        const pageSizeVal = pageSizeSelect ? pageSizeSelect.value : '5';
        const pageSize = pageSizeVal === 'all' ? cards.length : parseInt(pageSizeVal, 10) || 5;

        if (clearSearchBtn) {
            clearSearchBtn.style.display = query ? 'inline-flex' : 'none';
        }

        const matchingCards = [];
        cards.forEach(card => {
            const text = (card.getAttribute('data-search-text') || card.innerText || '').toLowerCase();
            const status = card.getAttribute('data-status') || '';
            const payment = card.getAttribute('data-payment-status') || '';

            let matchesQuery = !query || text.includes(query);
            let matchesStatus = !selectedStatus || status.toLowerCase() === selectedStatus.toLowerCase();
            let matchesPayment = true;

            if (selectedPayment) {
                if (selectedPayment === 'Pendiente') {
                    matchesPayment = payment.toLowerCase().includes('pend') || payment.toLowerCase().includes('sin pago');
                } else if (selectedPayment === 'Pago completado') {
                    matchesPayment = payment.toLowerCase().includes('completado');
                } else {
                    matchesPayment = payment.toLowerCase() === selectedPayment.toLowerCase();
                }
            }

            if (matchesQuery && matchesStatus && matchesPayment) {
                matchingCards.push(card);
            }
        });

        const totalMatching = matchingCards.length;
        const totalPages = Math.max(1, Math.ceil(totalMatching / pageSize));

        if (currentPage > totalPages) {
            currentPage = totalPages;
        }

        const startIndex = (currentPage - 1) * pageSize;
        const endIndex = pageSizeVal === 'all' ? totalMatching : Math.min(startIndex + pageSize, totalMatching);

        cards.forEach(card => {
            card.style.display = 'none';
        });

        matchingCards.forEach((card, idx) => {
            if (idx >= startIndex && idx < endIndex) {
                card.style.display = '';
            } else {
                card.style.display = 'none';
            }
        });

        if (totalMatching === 0) {
            if (noResultsDiv) noResultsDiv.style.display = 'flex';
            if (paginationBar) paginationBar.style.display = 'none';
            ordersList.style.display = 'none';
        } else {
            if (noResultsDiv) noResultsDiv.style.display = 'none';
            if (paginationBar) paginationBar.style.display = 'flex';
            ordersList.style.display = 'flex';
            ordersList.style.flexDirection = 'column';
        }

        if (paginationInfo) {
            if (totalMatching === 0) {
                paginationInfo.innerHTML = 'No hay pedidos que coincidan';
            } else {
                paginationInfo.innerHTML = `Mostrando <strong>${startIndex + 1} - ${endIndex}</strong> de <strong>${totalMatching}</strong> pedidos`;
            }
        }

        renderPaginationControls(totalPages);
    }

    function renderPaginationControls(totalPages) {
        if (!paginationControls) return;
        paginationControls.innerHTML = '';

        if (totalPages <= 1) return;

        const prevBtn = document.createElement('button');
        prevBtn.type = 'button';
        prevBtn.className = 'client-page-btn';
        prevBtn.innerHTML = '<i class="fa-solid fa-chevron-left"></i>';
        prevBtn.disabled = currentPage === 1;
        prevBtn.addEventListener('click', () => {
            if (currentPage > 1) {
                currentPage--;
                applyFiltersAndPaginate();
                scrollToTop();
            }
        });
        paginationControls.appendChild(prevBtn);

        for (let i = 1; i <= totalPages; i++) {
            if (totalPages > 7) {
                if (i !== 1 && i !== totalPages && Math.abs(i - currentPage) > 1) {
                    if (i === 2 && currentPage > 4) {
                        const dots = document.createElement('span');
                        dots.className = 'client-page-dots';
                        dots.innerText = '...';
                        paginationControls.appendChild(dots);
                    } else if (i === totalPages - 1 && currentPage < totalPages - 3) {
                        const dots = document.createElement('span');
                        dots.className = 'client-page-dots';
                        dots.innerText = '...';
                        paginationControls.appendChild(dots);
                    }
                    continue;
                }
            }

            const pageBtn = document.createElement('button');
            pageBtn.type = 'button';
            pageBtn.className = `client-page-btn ${i === currentPage ? 'active' : ''}`;
            pageBtn.innerText = i;
            pageBtn.addEventListener('click', () => {
                currentPage = i;
                applyFiltersAndPaginate();
                scrollToTop();
            });
            paginationControls.appendChild(pageBtn);
        }

        const nextBtn = document.createElement('button');
        nextBtn.type = 'button';
        nextBtn.className = 'client-page-btn';
        nextBtn.innerHTML = '<i class="fa-solid fa-chevron-right"></i>';
        nextBtn.disabled = currentPage === totalPages;
        nextBtn.addEventListener('click', () => {
            if (currentPage < totalPages) {
                currentPage++;
                applyFiltersAndPaginate();
                scrollToTop();
            }
        });
        paginationControls.appendChild(nextBtn);
    }

    function scrollToTop() {
        if (filterBar) {
            filterBar.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }

    if (searchInput) {
        searchInput.addEventListener('input', () => {
            currentPage = 1;
            applyFiltersAndPaginate();
        });
    }

    if (clearSearchBtn) {
        clearSearchBtn.addEventListener('click', () => {
            searchInput.value = '';
            currentPage = 1;
            applyFiltersAndPaginate();
        });
    }

    if (statusSelect) {
        statusSelect.addEventListener('change', () => {
            currentPage = 1;
            applyFiltersAndPaginate();
        });
    }

    if (paymentSelect) {
        paymentSelect.addEventListener('change', () => {
            currentPage = 1;
            applyFiltersAndPaginate();
        });
    }

    if (pageSizeSelect) {
        pageSizeSelect.addEventListener('change', () => {
            currentPage = 1;
            applyFiltersAndPaginate();
        });
    }

    if (resetFiltersBtn) {
        resetFiltersBtn.addEventListener('click', () => {
            if (searchInput) searchInput.value = '';
            if (statusSelect) statusSelect.value = '';
            if (paymentSelect) paymentSelect.value = '';
            if (pageSizeSelect) pageSizeSelect.value = '5';
            currentPage = 1;
            applyFiltersAndPaginate();
        });
    }

    applyFiltersAndPaginate();
});
