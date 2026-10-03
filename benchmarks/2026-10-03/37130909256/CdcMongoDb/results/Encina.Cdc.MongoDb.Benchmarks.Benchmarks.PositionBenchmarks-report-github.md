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
| ComparePositions | 132.94 ns |  3.986 ns | 0.218 ns | 12.50 |    0.61 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  10.66 ns | 10.745 ns | 0.589 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 618.98 ns | 19.641 ns | 1.077 ns | 58.18 |    2.82 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 572.65 ns | 10.039 ns | 0.550 ns | 53.83 |    2.61 | 0.0601 |    1008 B |       42.00 |
