
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error      | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|-----------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   366.9 μs | 1,589.8 μs | 87.14 μs |  1.67 |    0.39 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 2,640.4 μs |   422.6 μs | 23.16 μs | 12.02 |    1.37 | 2197.47 KB |       68.79 |
 AddAsync                |   155.3 μs |   370.2 μs | 20.29 μs |  0.71 |    0.11 |   23.56 KB |        0.74 |
 GetMessageAsync         |   222.5 μs |   575.2 μs | 31.53 μs |  1.01 |    0.17 |   31.95 KB |        1.00 |
