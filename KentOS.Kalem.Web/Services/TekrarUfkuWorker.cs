namespace KentOS.Kalem.Web.Services
{
    /// <summary>
    /// Sonsuz (UNTIL/COUNT içermeyen) tekrar serilerinin üretim ufkunu ileriye
    /// taşıyan arka plan görevi.
    ///
    /// NEDEN: Tekrarlar gerçek etkinlik satırı olarak üretilir; sonsuz bir kural
    /// için "hepsini" üretmek mümkün değildir. Bu yüzden 18 aylık bir ufka kadar
    /// üretilir ve zaman ilerledikçe bu görev ufku kaydırır. Böylece takvimde
    /// ileriye bakıldığında tekrarlar hazır bulunur.
    ///
    /// GÜVENLİK: Günde bir kez çalışır, hata durumunda yalnızca günlüğe yazar ve
    /// hiçbir kullanıcı işlemini etkilemez. Uygulama başlangıcında 2 dakika
    /// bekler — açılışta veritabanı göçü/ısınma ile yarışmasın.
    /// </summary>
    public class TekrarUfkuWorker(
        IServiceProvider _services,
        ILogger<TekrarUfkuWorker> _logger) : BackgroundService
    {
        private static readonly TimeSpan IlkBekleme = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan Periyot = TimeSpan.FromHours(24);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(IlkBekleme, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var seriService = scope.ServiceProvider.GetRequiredService<IAjandaSeriService>();
                    var uretilen = await seriService.UfkuGenisletAsync(stoppingToken);

                    if (uretilen > 0)
                    {
                        _logger.LogInformation("Tekrar ufku görevi {Adet} yeni tekrar üretti.", uretilen);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Tekrar ufku uzatılırken hata oluştu.");
                }

                try
                {
                    await Task.Delay(Periyot, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
