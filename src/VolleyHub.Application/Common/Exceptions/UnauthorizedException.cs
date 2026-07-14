namespace VolleyHub.Application.Common.Exceptions
{
    public sealed class UnauthorizedException : Exception
    {
        public UnauthorizedException()
            : base("User is not authenticated.")
        {
        }

        public UnauthorizedException(string message)
            : base(message)
        {
        }
    }
}