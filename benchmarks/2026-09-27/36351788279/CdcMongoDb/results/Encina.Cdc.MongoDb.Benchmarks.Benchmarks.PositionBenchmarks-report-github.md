```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 135.97 ns | 11.498 ns | 0.630 ns | 12.18 |    0.71 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  11.20 ns | 13.377 ns | 0.733 ns |  1.00 |    0.08 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 620.10 ns | 16.855 ns | 0.924 ns | 55.55 |    3.25 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 617.13 ns |  7.981 ns | 0.437 ns | 55.29 |    3.23 | 0.0601 |    1008 B |       42.00 |
