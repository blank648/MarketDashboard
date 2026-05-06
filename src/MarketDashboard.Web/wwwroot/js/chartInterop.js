window.chartInterop = {
    _chart: null,

    renderCandlestick: function (canvasId, labels, ohlcData, volumeData) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        if (this._chart) {
            this._chart.destroy();
            this._chart = null;
        }

        const upColor   = 'rgba(63,185,80,0.85)';
        const downColor = 'rgba(248,81,73,0.85)';
        const borderUp   = 'rgb(63,185,80)';
        const borderDown = 'rgb(248,81,73)';

        // Candlestick: use bar chart approximation
        // Each bar = Close price, colored green/red
        // True candlestick requires chartjs-chart-financial plugin
        // Use floating bar chart: [Low, High] with color based on Open<Close

        const barData = ohlcData.map(d => ({
            x: d.label,
            y: [d.low, d.high],
            open: d.open,
            close: d.close
        }));

        const colors = ohlcData.map(d =>
            d.close >= d.open ? upColor : downColor);
        const borderColors = ohlcData.map(d =>
            d.close >= d.open ? borderUp : borderDown);

        this._chart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Price Range (Low-High)',
                        data: ohlcData.map(d => [d.low, d.high]),
                        backgroundColor: colors,
                        borderColor: borderColors,
                        borderWidth: 1,
                        borderRadius: 2
                    },
                    {
                        label: 'Volume',
                        data: volumeData,
                        backgroundColor: 'rgba(88,166,255,0.2)',
                        borderColor: 'rgba(88,166,255,0.4)',
                        borderWidth: 1,
                        type: 'bar',
                        yAxisID: 'volume'
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: {
                        labels: { color: '#c9d1d9' }
                    },
                    tooltip: {
                        callbacks: {
                            label: function(ctx) {
                                const d = ohlcData[ctx.dataIndex];
                                if (!d) return '';
                                if (ctx.datasetIndex === 0) {
                                    return [
                                        `O: $${d.open.toFixed(2)}`,
                                        `H: $${d.high.toFixed(2)}`,
                                        `L: $${d.low.toFixed(2)}`,
                                        `C: $${d.close.toFixed(2)}`
                                    ];
                                }
                                return `Vol: ${ctx.parsed.y.toLocaleString()}`;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        ticks: { color: '#8b949e', maxTicksLimit: 10 },
                        grid: { color: 'rgba(255,255,255,0.05)' }
                    },
                    y: {
                        position: 'left',
                        ticks: { color: '#8b949e',
                                 callback: v => '$' + v.toFixed(0) },
                        grid: { color: 'rgba(255,255,255,0.05)' }
                    },
                    volume: {
                        position: 'right',
                        ticks: { color: '#8b949e',
                                 callback: v =>
                                     v >= 1e6 ? (v/1e6).toFixed(0)+'M'
                                     : v >= 1e3 ? (v/1e3).toFixed(0)+'K'
                                     : v },
                        grid: { drawOnChartArea: false }
                    }
                }
            }
        });
    },

    destroy: function () {
        if (this._chart) {
            this._chart.destroy();
            this._chart = null;
        }
    }
};
