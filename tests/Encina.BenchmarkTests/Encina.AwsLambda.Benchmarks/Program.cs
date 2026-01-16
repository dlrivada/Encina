using BenchmarkDotNet.Running;
using Encina.AwsLambda.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(ErrorCodeMappingBenchmarks).Assembly).Run(args);
