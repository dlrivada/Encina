```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 131.678 ns |  2.8929 ns | 0.1586 ns | 16.29 |    0.02 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.083 ns |  0.1844 ns | 0.0101 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 613.519 ns | 17.6407 ns | 0.9669 ns | 75.90 |    0.13 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 578.582 ns | 41.1173 ns | 2.2538 ns | 71.58 |    0.25 | 0.0601 |    1008 B |       42.00 |
