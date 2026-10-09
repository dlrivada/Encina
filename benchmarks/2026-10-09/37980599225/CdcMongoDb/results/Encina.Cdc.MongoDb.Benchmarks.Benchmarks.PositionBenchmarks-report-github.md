```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 136.14 ns | 10.88 ns | 0.596 ns |  9.45 |    1.14 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  14.61 ns | 37.72 ns | 2.067 ns |  1.01 |    0.17 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 665.62 ns | 64.11 ns | 3.514 ns | 46.18 |    5.58 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 617.06 ns | 17.39 ns | 0.953 ns | 42.81 |    5.17 | 0.0601 |    1008 B |       42.00 |
