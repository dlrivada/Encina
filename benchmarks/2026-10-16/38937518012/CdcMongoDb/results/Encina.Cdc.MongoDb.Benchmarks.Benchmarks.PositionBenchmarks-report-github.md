```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 107.960 ns | 31.174 ns | 1.7087 ns | 16.15 |    0.38 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.689 ns |  2.685 ns | 0.1472 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 485.825 ns | 76.014 ns | 4.1666 ns | 72.66 |    1.49 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 461.495 ns | 55.075 ns | 3.0188 ns | 69.02 |    1.37 | 0.0601 |    1008 B |       42.00 |
