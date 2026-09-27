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
| ComparePositions | 132.65 ns | 12.980 ns | 0.711 ns | 12.45 |    0.42 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  10.66 ns |  7.636 ns | 0.419 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 607.92 ns | 58.265 ns | 3.194 ns | 57.07 |    1.92 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 625.87 ns | 90.806 ns | 4.977 ns | 58.75 |    2.00 | 0.0601 |    1008 B |       42.00 |
