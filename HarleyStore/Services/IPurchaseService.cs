using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarleyStore.Models;

namespace HarleyStore.Services
{
    public interface IPurchaseService
    {
        Task<IEnumerable<Compra>> GetComprasAsync();
        Task<Compra> AddCompraAsync(Compra compra, Usuario usuario);
        Task<bool> AddAbonoAsync(Guid compraId, Abono abono, Usuario usuario);
    }
}
