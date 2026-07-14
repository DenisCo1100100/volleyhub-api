namespace VolleyHub.Application.Common.Exceptions
{
    public sealed class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException()
            : base("User is not allowed to perform this action.")
        {
        }

        public ForbiddenAccessException(string message)
            : base(message)
        {
        }
    }
}