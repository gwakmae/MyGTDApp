using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MyGtdApp.Models;
using MyGtdApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
using TaskStatus = MyGtdApp.Models.TaskStatus;

namespace MyGtdApp.Components.Pages
{
    // 이 파일은 기존의 방대한 Home.razor.cs 를 여러 partial 파일로 분리한 뒤
    // 구조 안내용 스텁(placeholder)으로 남겨둔 것입니다.
    // 실제 로직은 다음 파일들에 분산되어 있습니다:
    //
    //   - Home.State.Core.cs      : DI, 필드(상태) 단일 정의
    //   - Home.Lifecycle.cs       : 생명주기 & Dispose
    //   - Home.DataLoading.cs     : 라우트 기반 데이터 로드 & 빌드
    //   - Home.Selection.cs       : 단일/다중 선택 & Interop 키 처리
    //   - Home.BulkEdit.cs        : 벌크 업데이트/삭제
    //   - Home.ModalInterop.cs    : 모달 열기 JSInvokable
    //   - Home.Metrics.cs         : 통계/집계 유틸
    //   - (기존) Home.DragDrop.cs
    //   - (기존) Home.QuickAdd.cs
    //   - (기존) Home.TaskHandlers.cs
    //   - (기존) Home.UIState.cs
    //
    // 유지보수 시: 상태 필드가 필요한 새 기능은 Home.State.Core.cs 에 필드를 추가한 뒤
    // 관심사별 partial 파일을 새로 생성하거나 기존 파일에 추가해 주세요.
    public partial class Home
    {
    }
}