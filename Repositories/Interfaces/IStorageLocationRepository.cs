using System.Collections.Generic;
using CtrlEat.Models;

namespace CtrlEat.Repositories.Interfaces
{
    public interface IStorageLocationRepository
    {
        StorageLocation GetById(int id);
        List<StorageLocation> GetAll();
        List<StorageLocation> GetByName(string name);
        List<StorageLocation> GetByType(string type);
        int Create(StorageLocation location);
        void Update(StorageLocation location);
        void Delete(int id);
    }
}
