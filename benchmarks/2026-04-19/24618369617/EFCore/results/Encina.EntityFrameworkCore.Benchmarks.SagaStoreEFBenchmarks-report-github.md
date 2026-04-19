```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host] : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method   | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|--------- |-----:|------:|------:|--------:|------------:|
| GetAsync |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  SagaStoreEFBenchmarks.GetAsync: MediumRun(InvocationCount=1, IterationCount=15, LaunchCount=2, UnrollFactor=1, WarmupCount=10)
