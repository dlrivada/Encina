
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean | Error | Ratio | RatioSD | Alloc Ratio |
-------------------------- |-----:|------:|------:|--------:|------------:|
 'AddAsync single message' |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  OutboxStoreEFBenchmarks.'AddAsync single message': ShortRun(InvocationCount=1, IterationCount=3, LaunchCount=1, UnrollFactor=1, WarmupCount=3)
