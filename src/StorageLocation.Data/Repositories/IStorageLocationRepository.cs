using DomainStorageLocation = StorageLocation.Domain.Entities.StorageLocation;

namespace StorageLocation.Data.Repositories;

public interface IStorageLocationRepository
{
    Task<IReadOnlyList<DomainStorageLocation>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DomainStorageLocation>> FindAsync(
        string? name = null,
        string? type = null,
        CancellationToken cancellationToken = default);

    Task<DomainStorageLocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<DomainStorageLocation> CreateAsync(DomainStorageLocation location, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(DomainStorageLocation location, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
