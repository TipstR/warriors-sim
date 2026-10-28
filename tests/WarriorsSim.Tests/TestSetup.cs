using Xunit;

// The balancing values in Tuning are static, so all tests share them. Tests that ran at the same time could change
// each other's values, so they run one after another.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
