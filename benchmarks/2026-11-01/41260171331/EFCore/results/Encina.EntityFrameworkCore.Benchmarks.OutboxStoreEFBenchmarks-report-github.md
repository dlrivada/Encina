```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                    | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|-------------------------- |-----:|------:|------:|--------:|------------:|
| &#39;AddAsync single message&#39; |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  OutboxStoreEFBenchmarks.'AddAsync single message': MediumRun(InvocationCount=1, IterationCount=15, LaunchCount=2, UnrollFactor=1, WarmupCount=10)
