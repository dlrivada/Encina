```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.639 ns |   0.0941 ns |  0.0052 ns |   0.30 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   5.458 ns |   3.1902 ns |  0.1749 ns |   1.00 |    0.04 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 459.299 ns | 185.7296 ns | 10.1805 ns |  84.21 |    2.88 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 682.029 ns | 470.1598 ns | 25.7710 ns | 125.05 |    5.41 | 0.0296 |     504 B |       12.60 |
