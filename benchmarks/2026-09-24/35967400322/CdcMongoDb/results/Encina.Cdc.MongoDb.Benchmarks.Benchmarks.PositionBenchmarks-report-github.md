```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 137.83 ns |   5.071 ns | 0.278 ns |  9.59 |    0.81 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  14.47 ns |  27.026 ns | 1.481 ns |  1.01 |    0.12 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 620.76 ns | 102.713 ns | 5.630 ns | 43.18 |    3.65 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 621.50 ns | 110.516 ns | 6.058 ns | 43.23 |    3.66 | 0.0601 |    1008 B |       42.00 |
