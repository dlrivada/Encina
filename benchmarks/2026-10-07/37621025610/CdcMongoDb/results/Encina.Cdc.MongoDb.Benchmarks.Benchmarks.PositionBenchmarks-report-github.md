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
| ComparePositions | 137.57 ns |  1.239 ns | 0.068 ns | 10.54 |    0.25 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  13.06 ns |  6.609 ns | 0.362 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 626.84 ns | 52.480 ns | 2.877 ns | 48.01 |    1.17 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 621.35 ns | 62.887 ns | 3.447 ns | 47.59 |    1.17 | 0.0601 |    1008 B |       42.00 |
