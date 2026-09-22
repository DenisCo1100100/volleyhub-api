using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Infrastructure.Persistence.Repositories
{
    public sealed class GameTemplateRepository : IGameTemplateRepository
    {
        private readonly ApplicationDbContext _context;

        public GameTemplateRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GameTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.GameTemplates.SingleOrDefaultAsync(template => template.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<GameTemplate>> GetByOrganizerIdAsync(Guid organizerId, CancellationToken cancellationToken)
        {
            return await _context.GameTemplates.AsNoTracking().Where(template => template.OrganizerId == organizerId)
                .OrderBy(template => template.Name).ThenBy(template => template.Id).ToListAsync(cancellationToken);
        }

        public async Task AddAsync(GameTemplate template, CancellationToken cancellationToken)
        {
            await _context.GameTemplates.AddAsync(template, cancellationToken);
        }

        public void Update(GameTemplate template)
        {
            _context.GameTemplates.Update(template);
        }

        public void Delete(GameTemplate template)
        {
            _context.GameTemplates.Remove(template);
        }
    }
}
