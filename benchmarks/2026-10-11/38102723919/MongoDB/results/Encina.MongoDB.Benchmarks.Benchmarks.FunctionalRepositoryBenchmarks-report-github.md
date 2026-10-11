```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method       | Mean     | Error   | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|--------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 235.6 μs | 4.49 μs |  6.72 μs |  0.90 |    0.05 | 1.2207 |  20.55 KB |        0.91 |
| GetByIdAsync | 261.4 μs | 7.97 μs | 11.94 μs |  1.00 |    0.06 | 0.9766 |  22.52 KB |        1.00 |
