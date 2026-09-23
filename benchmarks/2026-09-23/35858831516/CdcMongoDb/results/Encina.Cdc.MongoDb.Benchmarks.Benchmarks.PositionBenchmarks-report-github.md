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
| ComparePositions | 135.37 ns |   8.876 ns | 0.487 ns | 10.30 |    0.83 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  13.23 ns |  23.431 ns | 1.284 ns |  1.01 |    0.12 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 619.21 ns |  79.294 ns | 4.346 ns | 47.10 |    3.78 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 590.55 ns | 120.301 ns | 6.594 ns | 44.92 |    3.62 | 0.0601 |    1008 B |       42.00 |
