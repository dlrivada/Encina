```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------- |---------:|-----------:|----------:|------:|--------:|----------:|------------:|
| AddAsync     | 909.2 μs | 4,277.4 μs | 234.46 μs |  4.05 |    0.91 |   6.63 KB |        1.04 |
| GetByIdAsync | 224.4 μs |   123.2 μs |   6.75 μs |  1.00 |    0.04 |   6.39 KB |        1.00 |
