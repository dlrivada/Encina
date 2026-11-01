```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method       | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 577.4 μs | 71.28 μs | 104.49 μs |  4.30 |    0.77 |      - |   6.64 KB |        1.04 |
| GetByIdAsync | 134.4 μs |  1.65 μs |   2.37 μs |  1.00 |    0.02 | 0.2441 |    6.4 KB |        1.00 |
