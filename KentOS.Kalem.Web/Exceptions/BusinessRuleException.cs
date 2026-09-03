namespace KentOS.Kalem.Web.Exceptions
{
    /// <summary>
    /// İş kuralı ihlali — istek biçimsel olarak doğru ama yapılmak istenen işlem
    /// kuralına aykırı (ör. gizli etkinliği havale etmek). HTTP 400 döner.
    ///
    /// <see cref="EntityNotFoundException"/> "kayıt yok" (404) demek için kullanılır;
    /// bu ise "kayıt var ama bu işlem yapılamaz" durumudur. İkisi de
    /// <c>EntityNotFoundExceptionFilter</c> içinde ele alınır — aksi hâlde mesaj
    /// 500 olarak dönerdi ve istemcide "sunucu hatası" gibi görünürdü.
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message)
        {
        }
    }
}
