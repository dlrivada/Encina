```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 137.117 ns |   9.638 ns | 0.5283 ns | 17.56 |    0.61 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   7.818 ns |   5.652 ns | 0.3098 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 580.545 ns |  87.731 ns | 4.8088 ns | 74.34 |    2.62 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 553.462 ns | 168.497 ns | 9.2359 ns | 70.87 |    2.65 | 0.0601 |    1008 B |       42.00 |
