```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------------ |---------:|---------:|--------:|------:|----------:|------------:|
| GetCurrentPositionAsync | 593.5 μs | 45.05 μs | 2.47 μs |  1.00 |    6.9 KB |        1.00 |
