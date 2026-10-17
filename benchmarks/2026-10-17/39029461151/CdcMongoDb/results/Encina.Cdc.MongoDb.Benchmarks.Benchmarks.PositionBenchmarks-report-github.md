```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.966 ns |  8.061 ns | 0.4418 ns | 17.60 |    0.30 | 0.0038 |      96 B |        4.00 |
| CreatePosition   |   7.670 ns |  2.722 ns | 0.1492 ns |  1.00 |    0.02 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 695.279 ns | 38.498 ns | 2.1102 ns | 90.68 |    1.53 | 0.0401 |    1024 B |       42.67 |
| ToBytes          | 687.114 ns | 33.035 ns | 1.8107 ns | 89.61 |    1.51 | 0.0401 |    1008 B |       42.00 |
