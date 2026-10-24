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
| ComparePositions |  71.989 ns |   4.410 ns | 0.2417 ns | 17.00 |    0.29 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.235 ns |   1.536 ns | 0.0842 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 314.385 ns | 124.918 ns | 6.8472 ns | 74.26 |    1.89 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 301.704 ns |  54.437 ns | 2.9839 ns | 71.26 |    1.36 | 0.0601 |    1008 B |       42.00 |
