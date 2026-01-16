using BenchmarkDotNet.Running;
using Encina.DistributedLock.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
