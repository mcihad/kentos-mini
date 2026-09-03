using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Dto
{
    public class ErrorCodes
    {
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string BadRequest = "BAD_REQUEST";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";
        public const string NotFound = "NOT_FOUND";
        public const string InternalServerError = "INTERNAL_SERVER_ERROR";
    }
}
