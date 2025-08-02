using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyGtdApp.Models;
using System.Text.Json;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        // Children 컬렉션은 DB에 저장하지 않음
        builder.Ignore(t => t.Children);

        // Contexts : JSON 직렬화 + Value Comparer 추가
        var jsonConv = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!);

        // ✅ Value Comparer 추가
        var valueComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        // ✅ 수정: 체이닝과 SetValueComparer 분리
        var property = builder.Property(t => t.Contexts)
                              .HasConversion(jsonConv)
                              .HasColumnType("TEXT");

        property.Metadata.SetValueComparer(valueComparer);

        builder.HasIndex(t => t.Path);
    }
}
