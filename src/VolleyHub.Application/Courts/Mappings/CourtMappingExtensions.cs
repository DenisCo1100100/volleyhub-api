using VolleyHub.Application.Courts.Dtos;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.Courts.Mappings
{
    public static class CourtMappingExtensions
    {
        public static CourtDto ToDto(this Court court)
        {
            return new CourtDto(
                court.Id,
                court.Name,
                court.Address,
                court.Latitude,
                court.Longitude,
                court.SurfaceType,
                court.IsIndoor,
                court.Description,
                court.CreatedAt,
                court.UpdatedAt);
        }
    }
}
