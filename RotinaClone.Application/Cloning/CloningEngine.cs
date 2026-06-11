using System;
using System.Threading;
using System.Threading.Tasks;
using RotinaClone.Domain.Interfaces;
using RotinaClone.Domain.Models;

namespace RotinaClone.Application.Cloning
{
    public class CloningEngine : ICloningEngine
    {
        private readonly SectorCloner _sectorCloner;
        private readonly IntelligentCloner _intelligentCloner;
        private CloneSession? _activeSession;

        public CloningEngine()
        {
            _sectorCloner = new SectorCloner();
            _intelligentCloner = new IntelligentCloner();
        }

        public async Task<CloneSession> StartCloneAsync(
            CloneOptions options, 
            Action<CloneSession> progressCallback, 
            CancellationToken cancellationToken)
        {
            if (_activeSession != null && (_activeSession.Status == "Running" || _activeSession.Status == "Verifying"))
            {
                throw new InvalidOperationException("A cloning session is already in progress.");
            }

            _activeSession = new CloneSession
            {
                Status = "Running",
                CurrentOperation = "Starting cloning engine..."
            };

            // Wrapped progress callback to track state locally
            Action<CloneSession> wrappedCallback = (s) =>
            {
                _activeSession = s;
                progressCallback(s);
            };

            try
            {
                if (options.IsSectorBySector)
                {
                    await _sectorCloner.ExecuteAsync(options, wrappedCallback, cancellationToken);
                }
                else
                {
                    await _intelligentCloner.ExecuteAsync(options, wrappedCallback, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _activeSession.Status = "Failed";
                _activeSession.CurrentOperation = $"Cloning process aborted: {ex.Message}";
                wrappedCallback(_activeSession);
            }

            return _activeSession;
        }

        public CloneSession? GetActiveSession()
        {
            return _activeSession;
        }
    }
}
