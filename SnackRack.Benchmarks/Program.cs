using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using SnackRack.Benchmarks;

IConfig config = args.Contains("--dry")
    ? new DebugInProcessConfig()
    : DefaultConfig.Instance;

BenchmarkSwitcher.FromAssembly(typeof(EmbeddingServiceBenchmarks).Assembly).RunAllJoined(config);
