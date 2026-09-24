
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------------ |----------:|----------:|---------:|------:|--------:|----------:|------------:|
 MarkAsProcessedAsync    | 185.51 μs | 172.18 μs | 9.438 μs |  1.15 |    0.05 |  11.97 KB |        1.20 |
 GetExpiredMessagesAsync | 764.39 μs |  72.28 μs | 3.962 μs |  4.73 |    0.03 | 287.61 KB |       28.72 |
 AddAsync                |  65.37 μs |  21.55 μs | 1.181 μs |  0.40 |    0.01 |   2.85 KB |        0.28 |
 GetMessageAsync         | 161.71 μs |  16.11 μs | 0.883 μs |  1.00 |    0.01 |  10.02 KB |        1.00 |
