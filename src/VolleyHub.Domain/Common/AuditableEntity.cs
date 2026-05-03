using System;

namespace VolleyHub.Domain.Common
{
    public abstract class AuditableEntity
    {
        public DateTimeOffset CreatedAt { get; protected set; }
        public DateTimeOffset? UpdateAt { get; protected set; }

        public void MarkCreated(DateTimeOffset now)
        {
            CreatedAt = now;
        }

        public void MarkUpdated(DateTimeOffset now)
        {
            UpdateAt = now;
        }
    }
}
