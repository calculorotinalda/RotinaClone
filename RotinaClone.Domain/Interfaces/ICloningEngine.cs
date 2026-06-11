using System;
using System.Threading;
using System.Threading.Tasks;
using RotinaClone.Domain.Models;

namespace RotinaClone.Domain.Interfaces
{
    public interface ICloningEngine
    {
        Task<CloneSession> StartCloneAsync(CloneOptions options, Action<CloneSession> progressCallback, CancellationToken cancellationToken);
        CloneSession? GetActiveSession();
    }
}
