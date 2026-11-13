```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 138.920 ns | 30.603 ns | 1.6775 ns | 16.76 |    0.46 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.296 ns |  4.443 ns | 0.2435 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 620.079 ns | 41.578 ns | 2.2790 ns | 74.79 |    1.90 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 582.101 ns | 60.019 ns | 3.2898 ns | 70.21 |    1.81 | 0.0601 |    1008 B |       42.00 |
