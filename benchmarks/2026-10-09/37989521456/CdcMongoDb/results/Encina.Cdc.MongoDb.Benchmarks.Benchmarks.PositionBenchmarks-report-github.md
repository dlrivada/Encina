```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.59 ns |  47.14 ns |  2.584 ns |  7.33 |    0.65 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  18.47 ns |  32.87 ns |  1.802 ns |  1.01 |    0.12 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 627.26 ns | 200.26 ns | 10.977 ns | 34.18 |    3.03 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 601.02 ns |  66.52 ns |  3.646 ns | 32.75 |    2.87 | 0.0601 |    1008 B |       42.00 |
