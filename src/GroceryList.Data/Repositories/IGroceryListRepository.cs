using DomainGroceryList = GroceryList.Domain.Entities.GroceryList;

namespace GroceryList.Data.Repositories;

public interface IGroceryListRepository
{
    Task<IReadOnlyList<DomainGroceryList>> GetAllAsync(
        DateTime? date = null,
        CancellationToken cancellationToken = default);

    Task<DomainGroceryList?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<DomainGroceryList> CreateAsync(
        DomainGroceryList groceryList,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(DomainGroceryList groceryList, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}