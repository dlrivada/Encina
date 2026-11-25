
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   227.0 μs | 449.7 μs | 24.65 μs |  1.21 |    0.15 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 2,519.4 μs | 589.9 μs | 32.33 μs | 13.46 |    1.00 | 2197.47 KB |       68.79 |
 AddAsync                |   122.5 μs | 389.6 μs | 21.35 μs |  0.65 |    0.11 |   23.56 KB |        0.74 |
 GetMessageAsync         |   188.2 μs | 296.6 μs | 16.26 μs |  1.00 |    0.11 |   31.95 KB |        1.00 |
