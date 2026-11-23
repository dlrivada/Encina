```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 155.0 μs |  37.01 μs | 2.03 μs |  1.08 |    0.04 | 0.9766 |  16.04 KB |        1.40 |
| GetByIdAsync | 144.1 μs | 104.35 μs | 5.72 μs |  1.00 |    0.05 | 0.4883 |  11.42 KB |        1.00 |
