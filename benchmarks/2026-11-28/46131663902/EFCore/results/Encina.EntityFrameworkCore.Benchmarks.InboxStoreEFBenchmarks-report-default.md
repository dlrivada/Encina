
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error      | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|-----------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   174.1 μs |   170.7 μs |  9.36 μs |  1.01 |    0.07 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 2,318.6 μs | 1,427.3 μs | 78.23 μs | 13.44 |    0.82 | 2197.47 KB |       68.79 |
 AddAsync                |   115.2 μs |   106.5 μs |  5.84 μs |  0.67 |    0.05 |   23.56 KB |        0.74 |
 GetMessageAsync         |   173.0 μs |   190.0 μs | 10.41 μs |  1.00 |    0.07 |   31.95 KB |        1.00 |
