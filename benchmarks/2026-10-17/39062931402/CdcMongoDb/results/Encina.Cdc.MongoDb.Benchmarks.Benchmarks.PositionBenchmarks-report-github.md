```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  67.754 ns |   5.4785 ns |  0.3003 ns | 15.70 |    0.11 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.314 ns |   0.5366 ns |  0.0294 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 328.350 ns | 293.5949 ns | 16.0929 ns | 76.11 |    3.26 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 296.261 ns |  21.9602 ns |  1.2037 ns | 68.67 |    0.47 | 0.0601 |    1008 B |       42.00 |
