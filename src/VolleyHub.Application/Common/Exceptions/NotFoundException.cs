using System;
using System.Collections.Generic;
using System.Text;

namespace VolleyHub.Application.Common.Exceptions
{
    public sealed class NotFoundException : Exception
    {
        public NotFoundException(string entityName, object key)
            : base($"{entityName} with key '{key}' was not found.")
        { 
        }
    }
}
