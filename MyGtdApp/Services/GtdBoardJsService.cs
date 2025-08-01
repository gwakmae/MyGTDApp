using Microsoft.JSInterop;

namespace MyGtdApp.Services;

public sealed class GtdBoardJsService : IGtdBoardJsService
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    public GtdBoardJsService(IJSRuntime js) => _js = js;

    private async ValueTask<IJSObjectReference> ModuleAsync()
        => _module ??= await _js.InvokeAsync<IJSObjectReference>(
                "import", "./js/gtd-board.js");

    public async ValueTask SetupAsync(DotNetObjectReference<object> helper)
        => await (await ModuleAsync()).InvokeVoidAsync("setup", helper);

    public async ValueTask DisposeAsync()
    {
        if (_module is not null) await _module.DisposeAsync();
    }
}
