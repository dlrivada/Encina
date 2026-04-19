```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host] : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                    | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|-------------------------- |-----:|------:|------:|--------:|------------:|
| &#39;AddAsync single message&#39; |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  OutboxStoreEFBenchmarks.'AddAsync single message': MediumRun(InvocationCount=1, IterationCount=15, LaunchCount=2, UnrollFactor=1, WarmupCount=10)
