```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| AddAsync     | 161.6 μs | 28.67 μs | 1.57 μs |  1.07 | 0.9766 |     16 KB |        1.40 |
| GetByIdAsync | 150.8 μs | 14.10 μs | 0.77 μs |  1.00 | 0.4883 |  11.39 KB |        1.00 |
