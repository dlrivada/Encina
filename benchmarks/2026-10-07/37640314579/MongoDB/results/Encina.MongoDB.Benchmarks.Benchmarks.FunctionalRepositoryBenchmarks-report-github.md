```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error     | StdDev  | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------- |---------:|----------:|--------:|------:|--------:|----------:|------------:|
| AddAsync     | 292.1 μs |  32.56 μs | 1.78 μs |  0.93 |    0.02 |  20.55 KB |        0.91 |
| GetByIdAsync | 313.7 μs | 110.45 μs | 6.05 μs |  1.00 |    0.02 |  22.52 KB |        1.00 |
