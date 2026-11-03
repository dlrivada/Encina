```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 135.068 ns |  4.286 ns | 0.2349 ns | 15.46 |    0.55 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.746 ns |  6.642 ns | 0.3641 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 640.800 ns | 13.503 ns | 0.7401 ns | 73.35 |    2.59 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 587.584 ns | 34.299 ns | 1.8800 ns | 67.26 |    2.38 | 0.0601 |    1008 B |       42.00 |
