using Microsoft.JSInterop;

namespace MyGtdApp.Services;

public interface IGtdBoardJsService : IAsyncDisposable
{
    ValueTask SetupAsync(DotNetObjectReference<object> helper);
}