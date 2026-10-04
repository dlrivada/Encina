```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 133.36 ns | 1.328 ns |  1.987 ns | 10.10 |    0.63 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  13.26 ns | 0.575 ns |  0.806 ns |  1.00 |    0.09 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 608.31 ns | 8.764 ns | 12.847 ns | 46.05 |    2.96 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 599.39 ns | 4.472 ns |  6.694 ns | 45.38 |    2.81 | 0.0601 |    1008 B |       42.00 |
