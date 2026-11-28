
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|-----------:|----------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   241.9 μs |   858.4 μs |  47.05 μs |  0.71 |    0.14 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 2,939.9 μs | 1,874.3 μs | 102.74 μs |  8.66 |    0.94 | 2197.47 KB |       68.79 |
 AddAsync                |   168.9 μs |   329.9 μs |  18.08 μs |  0.50 |    0.07 |   23.56 KB |        0.74 |
 GetMessageAsync         |   342.6 μs |   715.7 μs |  39.23 μs |  1.01 |    0.15 |   31.95 KB |        1.00 |
