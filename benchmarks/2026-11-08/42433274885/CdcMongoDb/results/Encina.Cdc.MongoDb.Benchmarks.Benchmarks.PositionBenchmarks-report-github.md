```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean       | Error     | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  73.728 ns | 1.1921 ns |  1.7096 ns | 16.11 |    0.57 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.581 ns | 0.0909 ns |  0.1274 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 344.104 ns | 5.9986 ns |  8.6030 ns | 75.17 |    2.75 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 324.914 ns | 6.6863 ns | 10.0078 ns | 70.98 |    2.89 | 0.0601 |    1008 B |       42.00 |
