namespace WordleStartOptimizer.Tests;

public class DataAssemblyFixture
{
    public DataAssemblyFixture()
    {
        Data.Initialize(Environment.ProcessorCount);
    }
}