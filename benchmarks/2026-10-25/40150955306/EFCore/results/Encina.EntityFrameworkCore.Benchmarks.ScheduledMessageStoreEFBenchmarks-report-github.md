```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host] : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method              | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|-------------------- |-----:|------:|------:|--------:|------------:|
| GetDueMessagesAsync |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  ScheduledMessageStoreEFBenchmarks.GetDueMessagesAsync: MediumRun(InvocationCount=1, IterationCount=15, LaunchCount=2, UnrollFactor=1, WarmupCount=10)
