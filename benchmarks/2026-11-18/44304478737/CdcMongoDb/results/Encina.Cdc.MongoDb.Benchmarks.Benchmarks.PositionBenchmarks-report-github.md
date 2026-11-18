```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 107.014 ns | 21.574 ns | 1.1826 ns | 15.61 |    0.33 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.856 ns |  2.726 ns | 0.1494 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 465.526 ns | 37.605 ns | 2.0613 ns | 67.92 |    1.31 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 453.437 ns | 96.615 ns | 5.2958 ns | 66.16 |    1.42 | 0.0601 |    1008 B |       42.00 |
