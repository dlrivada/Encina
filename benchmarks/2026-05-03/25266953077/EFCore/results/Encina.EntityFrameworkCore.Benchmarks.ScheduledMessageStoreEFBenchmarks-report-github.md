```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host] : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method              | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|-------------------- |-----:|------:|------:|--------:|------------:|
| GetDueMessagesAsync |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  ScheduledMessageStoreEFBenchmarks.GetDueMessagesAsync: MediumRun(InvocationCount=1, IterationCount=15, LaunchCount=2, UnrollFactor=1, WarmupCount=10)
