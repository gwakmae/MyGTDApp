using Microsoft.AspNetCore.Components.Server.Circuits;
using MyGtdApp.Services;

public class JsCleanupCircuitHandler : CircuitHandler
{
    private readonly IGtdBoardJsService _boardJs;
    public JsCleanupCircuitHandler(IGtdBoardJsService boardJs) => _boardJs = boardJs;

    public override async Task OnCircuitClosedAsync(Circuit circuit, CancellationToken ct)
        => await _boardJs.DisposeAsync();
}
