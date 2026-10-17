```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  76.723 ns | 12.739 ns | 0.6983 ns | 16.88 |    0.50 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.548 ns |  2.712 ns | 0.1487 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 347.547 ns | 53.260 ns | 2.9193 ns | 76.47 |    2.23 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 323.250 ns | 45.449 ns | 2.4912 ns | 71.12 |    2.07 | 0.0601 |    1008 B |       42.00 |
