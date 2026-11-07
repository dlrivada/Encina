```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.975 ns |   0.0785 ns |  0.0043 ns |   0.48 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.353 ns |   3.1664 ns |  0.1736 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   789.387 ns |  17.7955 ns |  0.9754 ns |  94.54 |    1.71 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,179.038 ns | 307.7587 ns | 16.8693 ns | 141.20 |    3.10 | 0.0286 |     504 B |       12.60 |
