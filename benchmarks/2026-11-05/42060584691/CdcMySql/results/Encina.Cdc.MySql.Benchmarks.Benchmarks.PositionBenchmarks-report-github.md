```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.971 ns |   0.0634 ns | 0.0035 ns |   0.42 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.362 ns |   4.7111 ns | 0.2582 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   794.366 ns | 119.8391 ns | 6.5688 ns |  84.89 |    2.09 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,169.660 ns |  27.5745 ns | 1.5115 ns | 125.00 |    2.95 | 0.0286 |     504 B |       12.60 |
