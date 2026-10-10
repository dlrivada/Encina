```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 111.547 ns |  35.666 ns | 1.9550 ns | 18.78 |    0.58 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   5.942 ns |   3.463 ns | 0.1898 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 448.713 ns | 153.904 ns | 8.4360 ns | 75.56 |    2.39 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 424.408 ns |  17.556 ns | 0.9623 ns | 71.47 |    1.95 | 0.0601 |    1008 B |       42.00 |
