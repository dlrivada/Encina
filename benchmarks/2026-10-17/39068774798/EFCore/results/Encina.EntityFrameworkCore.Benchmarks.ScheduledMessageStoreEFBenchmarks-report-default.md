
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                    | Mean        | Error      | StdDev    | Ratio | Allocated | Alloc Ratio |
-------------------------- |------------:|-----------:|----------:|------:|----------:|------------:|
 GetDueMessagesAsync       | 1,052.99 μs | 187.905 μs | 10.300 μs |  1.00 | 349.05 KB |       1.000 |
 AddAsync                  |    51.56 μs |   3.385 μs |  0.186 μs |  0.05 |   3.42 KB |       0.010 |
 RescheduleRecurringAsync  |   136.70 μs |  75.853 μs |  4.158 μs |  0.13 |  13.13 KB |       0.038 |
 MarkAsFailedAsync         |   144.02 μs | 197.202 μs | 10.809 μs |  0.14 |  13.03 KB |       0.037 |
 GetRecurringMessagesAsync |   627.25 μs | 247.810 μs | 13.583 μs |  0.60 | 166.55 KB |       0.477 |
