```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.464 ns |  1.069 ns | 0.0586 ns |   0.37 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.248 ns |  4.543 ns | 0.2490 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   706.664 ns | 41.130 ns | 2.2545 ns |  76.45 |    1.81 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,109.176 ns | 32.968 ns | 1.8071 ns | 120.00 |    2.83 | 0.0286 |     504 B |       12.60 |
