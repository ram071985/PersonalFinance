window.pfCharts = {
    _charts: {},

    renderCategorySpend: function (canvasId, labels, values) {
        const el = document.getElementById(canvasId);
        if (!el || typeof Chart === "undefined") return;

        if (this._charts[canvasId]) {
            this._charts[canvasId].destroy();
            delete this._charts[canvasId];
        }

        const ctx = el.getContext("2d");
        this._charts[canvasId] = new Chart(ctx, {
            type: "bar",
            data: {
                labels: labels,
                datasets: [{
                    label: "Spent",
                    data: values,
                    backgroundColor: "rgba(225, 29, 72, 0.75)",
                    borderColor: "rgba(225, 29, 72, 1)",
                    borderWidth: 1,
                    borderRadius: 6,
                    maxBarThickness: 36
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                const n = ctx.parsed.y ?? 0;
                                return "$" + n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        ticks: { color: "#9ca3af", maxRotation: 45, minRotation: 0 },
                        grid: { color: "rgba(255,255,255,0.06)" }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: {
                            color: "#9ca3af",
                            callback: function (v) { return "$" + v; }
                        },
                        grid: { color: "rgba(255,255,255,0.06)" }
                    }
                }
            }
        });
    },

    destroy: function (canvasId) {
        if (this._charts[canvasId]) {
            this._charts[canvasId].destroy();
            delete this._charts[canvasId];
        }
    }
};
