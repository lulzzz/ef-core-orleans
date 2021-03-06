using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Test.Platform.Wms.Core.Models;

namespace Test.Platform.Wms.Core.Interfaces
{
    public interface IInventoryRepository : ICrudRepo<Inventory>
    {
        Task<List<Inventory>> GetByItemId(Guid itemId, CancellationToken cancellationToken);
    }
}