```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| ComparePositions | 140.885 ns |  3.7181 ns | 0.2038 ns |  20.40 |    0.13 | 0.0038 |      96 B |        4.00 |
| CreatePosition   |   6.906 ns |  0.9323 ns | 0.0511 ns |   1.00 |    0.01 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 711.436 ns | 26.5987 ns | 1.4580 ns | 103.02 |    0.69 | 0.0401 |    1024 B |       42.67 |
| ToBytes          | 691.499 ns | 30.8544 ns | 1.6912 ns | 100.13 |    0.68 | 0.0401 |    1008 B |       42.00 |
