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
| ComparePositions | 131.68 ns |  6.461 ns | 0.354 ns | 10.65 |    0.56 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  12.40 ns | 13.641 ns | 0.748 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 596.62 ns | 39.564 ns | 2.169 ns | 48.23 |    2.52 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 580.94 ns | 56.382 ns | 3.090 ns | 46.96 |    2.46 | 0.0601 |    1008 B |       42.00 |
