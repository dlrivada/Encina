```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error     | StdDev  | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------- |---------:|----------:|--------:|------:|--------:|----------:|------------:|
| AddAsync     | 309.5 μs |  90.18 μs | 4.94 μs |  0.93 |    0.03 |  20.55 KB |        0.91 |
| GetByIdAsync | 333.1 μs | 173.14 μs | 9.49 μs |  1.00 |    0.03 |  22.52 KB |        1.00 |
