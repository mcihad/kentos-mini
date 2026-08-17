using System.Text.Json;
using KentOS.Mini.Application.Dto;
using Xunit;

namespace KentOS.Mini.Tests;

public class TokenDataDtoTests
{
    [Fact]
    public void ToJson_Mobil_Kontrati_String_Enum_Ile_Uretmeli()
    {
        var dto = new TokenDataDto(NotificationEntity.Ajanda, 5, NotificationAction.OpenDetails);

        var json = dto.ToJson();

        // JSON'u ayrıştırıp alanları ve tiplerini doğruluyoruz.
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("entity", out var entity));
        Assert.True(root.TryGetProperty("id", out var id));
        Assert.True(root.TryGetProperty("action", out var action));

        // entity ve action string enum olmalı (mobil kontratı).
        Assert.Equal(JsonValueKind.String, entity.ValueKind);
        Assert.Equal("Ajanda", entity.GetString());

        Assert.Equal(JsonValueKind.String, action.ValueKind);
        Assert.Equal("OpenDetails", action.GetString());

        // id sayısal olmalı.
        Assert.Equal(JsonValueKind.Number, id.ValueKind);
        Assert.Equal(5, id.GetInt32());
    }

    [Fact]
    public void FromJson_RoundTrip_Ayni_Degerleri_Korumali()
    {
        var original = new TokenDataDto(NotificationEntity.Talep, 42, NotificationAction.OpenImages);

        var json = original.ToJson();
        var geri = TokenDataDto.FromJson(json);

        Assert.NotNull(geri);
        Assert.Equal(original.Entity, geri!.Entity);
        Assert.Equal(original.Id, geri.Id);
        Assert.Equal(original.Action, geri.Action);
    }

    [Fact]
    public void FromJson_Mobil_String_Enum_Payloadi_Cozmeli()
    {
        // Mobil tarafın gönderdiği string-enum'lu ham JSON.
        const string mobilJson = "{\"entity\":\"Ajanda\",\"id\":5,\"action\":\"OpenDetails\"}";

        var dto = TokenDataDto.FromJson(mobilJson);

        Assert.NotNull(dto);
        Assert.Equal(NotificationEntity.Ajanda, dto!.Entity);
        Assert.Equal(5, dto.Id);
        Assert.Equal(NotificationAction.OpenDetails, dto.Action);
    }
}
