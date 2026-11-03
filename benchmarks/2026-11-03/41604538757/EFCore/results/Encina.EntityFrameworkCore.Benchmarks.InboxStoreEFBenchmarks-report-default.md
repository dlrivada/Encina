
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|-----------:|----------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   226.8 μs |   146.5 μs |   8.03 μs |  1.05 |    0.09 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 4,620.8 μs | 1,975.3 μs | 108.27 μs | 21.49 |    1.77 | 2189.73 KB |       68.55 |
 AddAsync                |   163.3 μs |   254.7 μs |  13.96 μs |  0.76 |    0.08 |   23.56 KB |        0.74 |
 GetMessageAsync         |   216.3 μs |   352.0 μs |  19.30 μs |  1.01 |    0.11 |   31.95 KB |        1.00 |
