
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

 Method     | Mean | Error | Ratio | RatioSD | Alloc Ratio |
----------- |-----:|------:|------:|--------:|------------:|
 SetContext |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  RequestContextAccessorBenchmarks.SetContext: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
