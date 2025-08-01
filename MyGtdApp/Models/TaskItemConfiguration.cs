using Microsoft.EntityFrameworkCore;
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

        // Contexts : JSON 직렬화
        var jsonConv = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!);

        builder.Property(t => t.Contexts)
               .HasConversion(jsonConv)
               .HasColumnType("TEXT");

        builder.HasIndex(t => t.Path);
    }
}
