// sale-timer.js
function updateSaleTimers() {
    document.querySelectorAll('.sale-timer').forEach(timer => {
        const endDateAttr = timer.getAttribute('data-end');
        if (!endDateAttr) return;

        const endDate = new Date(endDateAttr);
        const now = new Date();
        const distance = endDate - now;

        if (distance < 0) {
            timer.innerHTML = "Sale Ended";
            return;
        }

        const days = Math.floor(distance / (1000 * 60 * 60 * 24));
        const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((distance % (1000 * 60)) / 1000);

        timer.innerHTML = `${days}d ${hours}h ${minutes}m ${seconds}s`;
    });
}

// Run every se
