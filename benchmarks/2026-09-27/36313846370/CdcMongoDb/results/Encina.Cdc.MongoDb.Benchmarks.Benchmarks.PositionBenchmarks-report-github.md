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
| ComparePositions | 135.07 ns |  16.03 ns |  0.878 ns | 10.76 |    0.47 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  12.57 ns |  11.18 ns |  0.613 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 667.90 ns | 254.65 ns | 13.958 ns | 53.21 |    2.48 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 629.35 ns | 107.85 ns |  5.912 ns | 50.14 |    2.19 | 0.0601 |    1008 B |       42.00 |
