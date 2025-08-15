// 파일명: Components/Pages/Home.Lifecycle.cs (UPDATED)
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace MyGtdApp.Components.Pages;

public partial class Home : IAsyncDisposable
{
    protected override async Task OnInitializedAsync()
    {
        // 이벤트 핸들러: async void 지양 → 동기 핸들러 + 내부 fire-and-forget Task
        NavManager.LocationChanged += HandleLocationChanged;
        TaskService.OnChange += HandleTaskServiceChange;

        await RefreshDataBasedOnRoute();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadHideCompletedState();
            await LoadShowHiddenState();
            StateHasChanged();

            // DotNetObjectReference 재사용 + 누수 방지
            _dotNetRef = DotNetObjectReference.Create<object>(this);
            await BoardJs.SetupAsync(_dotNetRef);
            await JSRuntime.InvokeVoidAsync("setupKeyboardHandlers", _dotNetRef);
        }

        if (IsBoardView && boardContainerElement.Context != null)
        {
            await JSRuntime.InvokeVoidAsync("initializeColumnResizers", boardContainerElement);
        }
    }

    // 비동기 호출을 안전하게 시작하는 helper
    private void SafeRun(Func<Task> asyncFunc)
    {
        _ = InvokeAsync(async () =>
        {
            try { await asyncFunc(); }
            catch (JSDisconnectedException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex) { Console.WriteLine($"[Home] Async handler error: {ex.Message}"); }
        });
    }

    private void HandleTaskServiceChange()
        => SafeRun(RefreshDataBasedOnRoute);

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
        => SafeRun(RefreshDataBasedOnRoute);

    public async ValueTask DisposeAsync()
    {
        TaskService.OnChange -= HandleTaskServiceChange;
        NavManager.LocationChanged -= HandleLocationChanged;

        try
        {
            _ = JSRuntime.InvokeVoidAsync("cleanupKeyboardHandlers");
        }
        catch { /* JS 연결 종료 시 예외 무시 */ }

        if (_dotNetRef is not null)
        {
            _dotNetRef.Dispose();
            _dotNetRef = null;
        }

        try
        {
            await BoardJs.DisposeAsync();  // 모듈 정리 (이미 구현된 DisposeAsync)
        }
        catch { }
    }
}
