```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  70.800 ns |  14.855 ns | 0.8142 ns | 16.32 |    0.36 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.341 ns |   1.790 ns | 0.0981 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 336.040 ns | 137.075 ns | 7.5136 ns | 77.44 |    2.15 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 306.322 ns |  15.358 ns | 0.8418 ns | 70.59 |    1.41 | 0.0601 |    1008 B |       42.00 |
