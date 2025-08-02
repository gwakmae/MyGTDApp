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
        if (_module is null) return;

        // 연결이 살아 있을 때만 JS 호출
        if (_js is IJSInProcessRuntime ||     // WebAssembly 모드
            (_js as IJSRuntime is not null && // Server 모드
             ((IJSUnmarshalledRuntime?)_js) is not null))
        {
            await _module.DisposeAsync();
        }

        _module = null;
    }
}
