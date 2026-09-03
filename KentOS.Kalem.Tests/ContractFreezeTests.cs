using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using KentOS.Kalem.Web.Data;

namespace KentOS.Kalem.Tests;

/// <summary>
/// SÖZLEŞME DONDURMA — veritabanı kolonları ve JSON alan adları.
/// </summary>
/// <remarks>
/// <para>
/// Kod tabanı Türkçe'den İngilizce'ye çevriliyor. Bu çeviri iki sözleşmeyi
/// <b>sessizce</b> kırabilir çünkü ikisi de C# property adından TÜRETİLİYOR:
/// </para>
/// <list type="number">
///   <item><b>Kolon adı</b> — <c>UseSnakeCaseNamingConvention()</c>
///   <c>Konu</c>'yu <c>konu</c> kolonuna çeviriyor. Property
///   <c>Subject</c> olursa EF canlı tabloda <b>olmayan</b> <c>subject</c>
///   kolonunu arar. Uygulama açılışta değil, o sorgu ilk çalıştığında
///   patlar.</item>
///   <item><b>JSON alan adı</b> — serileştirici camelCase uyguluyor:
///   <c>Konu</c> → <c>"konu"</c>. <c>Subject</c> → <c>"subject"</c>, yani
///   <b>canlı Flutter uygulaması</b> alanı <c>null</c> okur. 500 bile
///   almazsın; ekran boş açılır ve kimse fark etmez.</item>
/// </list>
/// <para>
/// Bu testler ikisinin de anlık görüntüsünü depoda tutuyor. Bir yeniden
/// adlandırma adlardan birini değiştirirse test kırmızıya döner — hata
/// kullanıcıya değil derleme hattına düşer.
/// </para>
/// <para>
/// <b>Anlık görüntü yalnızca KASITLI sözleşme değişikliğinde güncellenir</b>
/// ve gerekçesi commit mesajına yazılır. Testi susturmak için güncellemek,
/// bekçiyi kapatmakla aynı şey.
/// </para>
/// </remarks>
public class ContractFreezeTests
{
    // ─────────────────────────── veritabanı ───────────────────────────

    [Fact]
    public void DatabaseColumnNamesAreFrozen()
    {
        var current = BuildDatabaseSnapshot();
        AssertMatchesSnapshot("database-contract.txt", current);
    }

    /// <summary>
    /// EF modelinden tablo ve kolon adlarını okur.
    /// </summary>
    /// <remarks>
    /// Veritabanına BAĞLANMAZ: model, sağlayıcı yapılandırmasından derleniyor.
    /// Postgres olmayan makinede de koşabilmesi önemli — sözleşme bekçisi,
    /// koşullu çalışan bir test olamaz.
    /// </remarks>
    private static string BuildDatabaseSnapshot()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=sozlesme_yok;Username=x;Password=x")
            .UseSnakeCaseNamingConvention()
            .Options;

        using var db = new AppDbContext(options);
        var sb = new StringBuilder();

        // SIRALAMA TABLO ADINA GÖRE, CLR tür adına göre DEĞİL.
        //
        // Önceden `e.Name` (tam nitelikli tür adı) kullanılıyordu ve bu, anlık
        // görüntüyü AD ALANINA bağımlı kılıyordu: proje `Sivasbeltr.WorkCollab`
        // → `KentOS.Kalem` olarak yeniden adlandırıldığında tek bir kolon
        // değişmediği hâlde test kırmızıya döndü, çünkü Identity türleriyle
        // uygulama türlerinin göreli sırası değişti.
        //
        // Bekçinin işi VERİTABANI adlarını korumak; kod tarafındaki bir
        // yeniden adlandırmanın onu kırma yetkisi yok.
        foreach (var entity in db.Model.GetEntityTypes()
                     .Where(e => e.GetTableName() is not null)
                     .OrderBy(e => e.GetTableName(), StringComparer.Ordinal)
                     .ThenBy(e => e.GetSchema() ?? string.Empty, StringComparer.Ordinal))
        {
            var table = entity.GetTableName();
            if (table is null) continue;

            var schema = entity.GetSchema();
            sb.Append("TABLE ").Append(table);
            if (!string.IsNullOrEmpty(schema)) sb.Append(" schema=").Append(schema);
            sb.AppendLine();

            var id = StoreObjectIdentifier.Table(table, schema);

            foreach (var property in entity.GetProperties()
                         .Select(p => (Clr: p.Name, Column: p.GetColumnName(id)))
                         .Where(x => x.Column is not null)
                         .OrderBy(x => x.Column, StringComparer.Ordinal))
            {
                // CLR adı BİLEREK yazılmıyor: bu dosyanın işi kolon adını
                // dondurmak. CLR adı da yazılsaydı her yeniden adlandırma
                // anlık görüntüyü değiştirir ve bekçi gürültüye boğulurdu.
                sb.Append("  ").AppendLine(property.Column);
            }
        }

        return sb.ToString();
    }

    // ───────────────────────────── JSON ─────────────────────────────

    [Fact]
    public void MobileJsonFieldNamesAreFrozen()
    {
        var current = BuildJsonSnapshot();
        AssertMatchesSnapshot("json-contract.txt", current);
    }

    /// <summary>
    /// v1 DTO'larının JSON alan adları — <b>canlı mobil uygulamanın gördüğü şey</b>.
    /// </summary>
    /// <remarks>
    /// Kapsam <c>Application.Dto</c> (v1) ve <c>Application.Dto.V2</c>: mobil
    /// uygulama v1'in yanında bazı v2 uçlarını da çağırıyor, yani ikisi de
    /// sözleşme. SPA v2 tiplerini yeniden üretiyor ama mobil üretmiyor.
    /// </remarks>
    private static string BuildJsonSnapshot()
    {
        var assembly = typeof(Application.Models.Randevu).Assembly;
        var sb = new StringBuilder();

        var types = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true })
            .Where(t => t.Namespace?.StartsWith("KentOS.Kalem.Application.Dto", StringComparison.Ordinal) == true)
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        foreach (var type in types)
        {
            sb.Append("DTO ").AppendLine(type.FullName);

            foreach (var name in type
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(p => p.GetMethod is not null)
                         .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
                         .Select(JsonFieldName)
                         .OrderBy(n => n, StringComparer.Ordinal))
            {
                sb.Append("  ").AppendLine(name);
            }
        }

        return sb.ToString();
    }

    /// <summary>Serileştiricinin bu property için yazacağı alan adı.</summary>
    private static string JsonFieldName(PropertyInfo property)
    {
        var açık = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (açık is not null) return açık.Name;

        // ASP.NET Core MVC varsayılanı: camelCase. `Program.cs` yalnızca
        // ReferenceHandler ayarlıyor, adlandırma politikasına dokunmuyor.
        return JsonNamingPolicy.CamelCase.ConvertName(property.Name);
    }

    // ─────────────────────────── karşılaştırma ───────────────────────────

    private static void AssertMatchesSnapshot(string fileName, string current)
    {
        var path = Path.Combine(ProjectDirectory(), "Contracts", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (!File.Exists(path))
        {
            File.WriteAllText(path, current);
            Assert.Fail(
                $"Sözleşme anlık görüntüsü YOKTU, oluşturuldu: {path}\n" +
                "İçeriği gözden geçirip depoya ekleyin ve testi yeniden koşun.");
        }

        var expected = Normalize(File.ReadAllText(path));
        var actual = Normalize(current);

        // `==` DİZİLERDE referans karşılaştırır, içerik değil: aynı dosya
        // kendisiyle karşılaştırıldığında bile "değişti" diyordu ve
        // farkı yazdıran blok boş çıkıyordu (hiçbir satır eksik/fazla değil).
        if (expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            File.Delete(path + ".actual");
            return;
        }

        var diff = Describe(expected, actual);
        File.WriteAllText(path + ".actual", current);

        Assert.Fail(
            $"SÖZLEŞME DEĞİŞTİ — {fileName}\n\n{diff}\n\n" +
            "Bu adlar canlı veritabanının kolonları ve canlı mobil uygulamanın\n" +
            "okuduğu JSON alanları. Bir yeniden adlandırma yaptıysanız DOĞRU\n" +
            "çözüm anlık görüntüyü güncellemek değil, adı sabitlemektir:\n" +
            "  • kolon için  [Column(\"eski_ad\")] ya da HasColumnName(\"eski_ad\")\n" +
            "  • JSON için   [JsonPropertyName(\"eskiAd\")]\n\n" +
            $"Üretilen güncel hâl: {path}.actual");
    }

    private static string[] Normalize(string text) =>
        text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

    private static string Describe(string[] expected, string[] actual)
    {
        // `Except` KÜME işlemidir: `id` kolonu 47 tabloda geçtiği için
        // tekrarları yutar ve sıra değişimini hiç göstermez. Karşılaştırma
        // bu yüzden çokluk koruyarak yapılıyor.
        var missing = Fark(expected, actual);
        var added = Fark(actual, expected);

        var sb = new StringBuilder();
        if (missing.Count > 0)
        {
            sb.AppendLine("KAYBOLAN (sözleşmeyi kıran):");
            foreach (var line in missing) sb.Append("  − ").AppendLine(line.Trim());
        }
        if (added.Count > 0)
        {
            sb.AppendLine("EKLENEN:");
            foreach (var line in added) sb.Append("  + ").AppendLine(line.Trim());
        }
        return sb.ToString();
    }

    /// <summary>`a` içinde olup `b` içinde OLMAYAN satırlar — tekrarları koruyarak.</summary>
    private static List<string> Fark(string[] a, string[] b)
    {
        var kalan = new List<string>(b);
        var sonuc = new List<string>();
        foreach (var satir in a)
        {
            if (!kalan.Remove(satir)) sonuc.Add(satir);
            if (sonuc.Count >= 25) break;
        }
        return sonuc;
    }

    /// <summary>Test projesinin kaynak dizini (bin/ değil).</summary>
    private static string ProjectDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.EnumerateFiles("*.csproj").Any())
        {
            dir = dir.Parent;
        }
        return dir?.FullName
               ?? throw new InvalidOperationException("Test projesi dizini bulunamadı.");
    }
}
