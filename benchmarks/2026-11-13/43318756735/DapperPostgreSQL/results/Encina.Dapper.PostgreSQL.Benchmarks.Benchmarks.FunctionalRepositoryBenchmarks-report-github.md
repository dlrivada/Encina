```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| AddAsync     | 156.4 μs | 24.04 μs | 1.32 μs |  1.05 | 0.7324 |  15.37 KB |        1.19 |
| GetByIdAsync | 149.4 μs |  4.82 μs | 0.26 μs |  1.00 | 0.7324 |  12.97 KB |        1.00 |
