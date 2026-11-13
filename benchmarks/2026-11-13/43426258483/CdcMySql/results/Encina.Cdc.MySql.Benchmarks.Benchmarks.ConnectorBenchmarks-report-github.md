```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------------ |---------:|--------:|--------:|------:|----------:|------------:|
| GetCurrentPositionAsync | 598.5 μs | 8.16 μs | 0.45 μs |  1.00 |   6.92 KB |        1.00 |
