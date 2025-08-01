using Microsoft.EntityFrameworkCore;
using MyGtdApp.Services;   // GtdDbContext
using MyGtdApp.Models;

namespace MyGtdApp.Infrastructure.Seeders
{
    public static class FillPathDepth
    {
        /* ★ 추가 : 0-패딩 유틸리티 --------------------- */
        private static string Pad(int n) => n.ToString("D6");
        /* --------------------------------------------- */

        public static async Task RunAsync(GtdDbContext db)
        {
            // 이미 한 번이라도 채워졌으면 건너뜀
            if (await db.Tasks.AnyAsync(t => t.Path != "")) return;

            // 모든 Task를 가져옴(추적 안 함)
            var list = await db.Tasks.AsNoTracking()
                                     .OrderBy(t => t.Id)
                                     .ToListAsync();
            var map = list.ToDictionary(t => t.Id);

            foreach (var t in list)
            {
                if (t.ParentId == null)          // 루트 노드
                {
                    t.Path = Pad(t.Id);         // 예: 000004
                    t.Depth = 0;
                }
                else                             // 자식 노드
                {
                    var p = map[t.ParentId.Value];
                    t.Path = $"{p.Path}/{Pad(t.Id)}"; // 예: 000004/000102
                    t.Depth = p.Depth + 1;
                }
            }

            db.UpdateRange(list);      // 변경 사항 반영
            await db.SaveChangesAsync();
        }
    }
}
