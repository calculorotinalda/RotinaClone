namespace RotinaClone.Domain.Models
{
    public class CloneOptions
    {
        public int SourceDiskIndex { get; set; } = -1;
        public int DestinationDiskIndex { get; set; } = -1;
        
        // True = Copy all sectors, False = Copy only used sectors
        public bool IsSectorBySector { get; set; } = false;
        public bool IsIntelligent { get; set; } = true;
        
        // Auto-scale partition boundaries if source and target have different sizes
        public bool ResizePartitions { get; set; } = true;
        
        // Volume Shadow Copy Service for live hot-cloning
        public bool UseVss { get; set; } = true;
        
        // Auto-align sectors on 4K boundaries (highly recommended for SSDs)
        public bool Align4K { get; set; } = true;

        // Post-clone integrity verification via SHA-256 block hashing
        public bool VerifyIntegrity { get; set; } = true;

        // Simulation mode (read only, do not write sectors)
        public bool IsSimulation { get; set; } = true;
    }
}
