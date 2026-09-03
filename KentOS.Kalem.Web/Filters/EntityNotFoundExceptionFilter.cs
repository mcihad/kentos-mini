using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using KentOS.Kalem.Web.Exceptions;
using KentOS.Kalem.Application.Dto;

namespace KentOS.Kalem.Web.Filters
{
    public class EntityNotFoundExceptionFilter : IActionFilter, IOrderedFilter
    {
        public int Order => int.MaxValue - 10;

        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Başka bir filtre bu istisnayı zaten karşıladıysa DOKUNMA.
            //
            // NEDEN: Bu filtre GLOBAL ve son dalında `is Exception` yakalıyor,
            // yani her şeyi. `/api/v2` kendi ProblemDetails yanıtını üretiyor;
            // bu kontrol olmadan v2'nin yanıtı burada sessizce ezilirdi.
            // Bugün bu bayrağı ondan önce kimse set etmediği için v1 davranışı
            // bit düzeyinde aynı kalır.
            if (context.ExceptionHandled)
            {
                return;
            }

            if (context.Exception is EntityNotFoundException notFoundException)
            {
                context.Result = new ObjectResult(new ErrorResponseDto { Code = ErrorCodes.NotFound, Message = notFoundException.Message })
                {
                    StatusCode = (int)System.Net.HttpStatusCode.NotFound
                };

                context.ExceptionHandled = true;
            }
            else if (context.Exception is BusinessRuleException businessRuleException)
            {
                // İş kuralı ihlali istemciye 400 + anlaşılır mesaj olarak döner;
                // 500 dönmesi istemcide "sunucu hatası" gibi görünürdü.
                context.Result = new ObjectResult(new ErrorResponseDto { Code = ErrorCodes.BadRequest, Message = businessRuleException.Message })
                {
                    StatusCode = (int)System.Net.HttpStatusCode.BadRequest
                };

                context.ExceptionHandled = true;
            }
            else if (context.Exception is Exception exception)
            {
                context.Result = new ObjectResult(new ErrorResponseDto { Code = ErrorCodes.InternalServerError, Message = exception.Message })
                {
                    StatusCode = 500
                };
                context.ExceptionHandled = true;
            }
        }
    }

}
