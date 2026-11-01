
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   208.1 μs |  3.03 μs |  4.04 μs |  1.12 |    0.04 |   34.19 KB |        1.06 |
 GetExpiredMessagesAsync | 4,449.1 μs | 32.18 μs | 45.11 μs | 23.95 |    0.80 | 2189.73 KB |       67.92 |
 AddAsync                |   123.4 μs |  4.17 μs |  5.85 μs |  0.66 |    0.04 |   23.56 KB |        0.73 |
 GetMessageAsync         |   186.0 μs |  4.52 μs |  6.19 μs |  1.00 |    0.05 |   32.24 KB |        1.00 |
