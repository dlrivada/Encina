
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method     | Mean | Error | Ratio | RatioSD | Alloc Ratio |
----------- |-----:|------:|------:|--------:|------------:|
 SetContext |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  RequestContextAccessorBenchmarks.SetContext: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
