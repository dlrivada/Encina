```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.92GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method       | Mean     | Error   | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------- |---------:|--------:|---------:|------:|--------:|----------:|------------:|
| AddAsync     | 291.0 μs | 8.32 μs | 11.94 μs |  0.88 |    0.05 |  20.55 KB |        0.91 |
| GetByIdAsync | 331.0 μs | 9.36 μs | 14.02 μs |  1.00 |    0.06 |  22.52 KB |        1.00 |
