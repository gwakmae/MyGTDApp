using Microsoft.JSInterop;

public class SidebarJsService : ISidebarJsService, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    public SidebarJsService(IJSRuntime js) => _js = js;

    public async ValueTask<bool> ToggleAsync()
    {
        _module ??= await _js.InvokeAsync<IJSObjectReference>(
                        "import", "./js/sidebar.js");
        return await _module.InvokeAsync<bool>("toggleSidebar");
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null) await _module.DisposeAsync();
    }
}
