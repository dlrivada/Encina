
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method              | Mean | Error | Ratio | RatioSD | Alloc Ratio |
-------------------- |-----:|------:|------:|--------:|------------:|
 GetDueMessagesAsync |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  ScheduledMessageStoreEFBenchmarks.GetDueMessagesAsync: ShortRun(InvocationCount=1, IterationCount=3, LaunchCount=1, UnrollFactor=1, WarmupCount=3)
