# SpreadTrader operational checks

`spreadtrader-health.ps1` is a read-only liveness probe. It does not restart
the application, subscribe to a market, place an order, or change settings.

It records one JSON line per run under `C:\Temp\SpreadTrader\health.jsonl` and
checks both the `SpreadTraderV2` process and the canonical stream proxy on
port 18088. A zero exit code means both are reachable; a non-zero exit code
requires operator review. It is suitable for a one-minute Windows scheduled
task, but the scheduled task should be added separately after a controlled
test.

The offline order lifecycle smoke test is enabled only when the process is
started with `SPREADTRADER_SIM_SMOKE=1`. Select a market with a runner and it
will exercise synthetic placement, stream display, partial fill and
cancellation; it never calls the Betfair order API.

Example:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\ops\spreadtrader-health.ps1
```
