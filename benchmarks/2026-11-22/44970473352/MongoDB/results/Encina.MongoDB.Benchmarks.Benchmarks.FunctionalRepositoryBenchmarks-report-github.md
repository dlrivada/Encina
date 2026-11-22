```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 3.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method       | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 255.3 μs | 5.71 μs | 8.55 μs |  0.89 |    0.04 | 0.9766 |  20.55 KB |        0.91 |
| GetByIdAsync | 288.6 μs | 4.79 μs | 6.72 μs |  1.00 |    0.03 | 0.9766 |  22.52 KB |        1.00 |
