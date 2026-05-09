// marketApiWidget.js — JS API Integration Widget
// Consumes /api/symbols (public) + /api/prices?symbol=X (authenticated)
// Demonstrates: fetch, loading/error handling, DOM manipulation

const MarketWidget = (function () {

  // Render a loading skeleton
  function showLoading(container) {
    container.innerHTML = `
      <div class="widget-loading">
        <div class="widget-spinner"></div>
        <span>Loading market data...</span>
      </div>`;
  }

  // Render error state
  function showError(container, message) {
    container.innerHTML = `
      <div class="widget-error">
        <span class="widget-error-icon">⚠</span>
        <span>${message}</span>
        <button onclick="MarketWidget.init()" 
                class="widget-retry">Retry</button>
      </div>`;
  }

  // Format price with 2 decimals
  function fmt(n) {
    return Number(n).toLocaleString('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
  }

  // Format large numbers (volume)
  function fmtVol(n) {
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M';
    if (n >= 1_000)     return (n / 1_000).toFixed(0) + 'K';
    return n.toString();
  }

  // Render data table
  function renderTable(container, data) {
    if (!data || data.length === 0) {
      container.innerHTML =
        '<p class="widget-empty">No market data available.</p>';
      return;
    }

    const rows = data.map(item => `
      <tr>
        <td class="widget-ticker">${item.symbol ?? item.ticker}</td>
        <td class="widget-price">$${fmt(item.price)}</td>
        <td class="widget-volume">${fmtVol(item.volume ?? 0)}</td>
        <td class="widget-time">
          ${new Date(item.recordedAt ?? item.createdAt)
              .toLocaleTimeString('en-US', { hour: '2-digit',
                                             minute: '2-digit' })}
        </td>
      </tr>`).join('');

    container.innerHTML = `
      <div class="widget-header">
        <span class="widget-title">📊 Live Market Snapshot</span>
        <span class="widget-updated">
          Updated: ${new Date().toLocaleTimeString()}
        </span>
      </div>
      <table class="widget-table">
        <thead>
          <tr>
            <th>Symbol</th>
            <th>Price</th>
            <th>Volume</th>
            <th>As Of</th>
          </tr>
        </thead>
        <tbody>${rows}</tbody>
      </table>
      <p class="widget-source">
        Source: <code>GET /api/prices/latest</code>
      </p>`;
  }

  // Main fetch function
  async function fetchAndRender() {
    const container = document.getElementById('market-api-widget');
    if (!container) return;

    showLoading(container);

    try {
      // Try authenticated endpoint first
      let response = await fetch('/api/prices/latest', {
        credentials: 'include',   // send Identity cookie
        headers: { 'Accept': 'application/json' }
      });

      // If 401 (not logged in), fall back to public /api/symbols
      if (response.status === 401) {
        response = await fetch('/api/symbols', {
          headers: { 'Accept': 'application/json' }
        });

        if (!response.ok) {
          throw new Error(
            `API error: ${response.status} ${response.statusText}`);
        }

        const symbols = await response.json();
        // Map symbols to same shape as price data
        const mapped = symbols.map(s => ({
          symbol: s.ticker,
          price: 0,
          volume: 0,
          recordedAt: s.createdAt,
          _noPrice: true
        }));
        renderTable(container, mapped);
        return;
      }

      if (!response.ok) {
        const errJson = await response.json().catch(() => null);
        throw new Error(
          errJson?.message ??
          `API error: ${response.status} ${response.statusText}`
        );
      }

      const data = await response.json();
      renderTable(container, data);

    } catch (err) {
      showError(container, err.message ?? 'Failed to load data.');
    }
  }

  // Auto-refresh every 30 seconds
  let _intervalId = null;

  function init() {
    fetchAndRender();
    if (_intervalId) clearInterval(_intervalId);
    _intervalId = setInterval(fetchAndRender, 30_000);
  }

  function destroy() {
    if (_intervalId) {
      clearInterval(_intervalId);
      _intervalId = null;
    }
  }

  return { init, destroy };
})();
