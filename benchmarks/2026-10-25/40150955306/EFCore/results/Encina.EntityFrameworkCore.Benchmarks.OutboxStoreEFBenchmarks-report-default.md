
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    34.60 μs |  2.651 μs |  3.629 μs |  1.01 |    0.14 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   444.37 μs | 13.667 μs | 20.032 μs | 12.97 |    1.34 |  146.48 KB |       51.23 |
 'GetPendingMessagesAsync batch=100' | 3,090.26 μs | 52.259 μs | 74.948 μs | 90.17 |    8.69 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   136.89 μs |  9.084 μs | 12.734 μs |  3.99 |    0.52 |   11.91 KB |        4.16 |
