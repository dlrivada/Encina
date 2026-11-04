```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 133.78 ns |  4.001 ns | 0.219 ns | 12.31 |    0.74 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  10.91 ns | 14.427 ns | 0.791 ns |  1.00 |    0.09 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 627.17 ns | 20.180 ns | 1.106 ns | 57.69 |    3.48 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 612.32 ns |  6.448 ns | 0.353 ns | 56.32 |    3.39 | 0.0601 |    1008 B |       42.00 |
