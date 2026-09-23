
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean        | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |------------:|-----------:|----------:|------:|--------:|----------:|------------:|
 GetDueMessagesAsync       | 1,061.53 μs | 306.336 μs | 16.791 μs |  1.00 |    0.02 | 349.05 KB |       1.000 |
 AddAsync                  |    54.34 μs |   8.312 μs |  0.456 μs |  0.05 |    0.00 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  |   162.56 μs | 322.112 μs | 17.656 μs |  0.15 |    0.01 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         |   143.68 μs | 212.056 μs | 11.624 μs |  0.14 |    0.01 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync |   608.26 μs | 381.686 μs | 20.921 μs |  0.57 |    0.02 | 166.55 KB |       0.477 |
