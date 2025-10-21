namespace ShipmentTracker.Core.Interfaces;

public interface IDataSeedingService
{
    /// <summary>
    /// Seeds the database with initial data if tables are empty
    /// </summary>
    /// <returns>Task representing the seeding operation</returns>
    Task SeedAsync();
}
