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
| ComparePositions | 133.30 ns | 22.38 ns | 1.227 ns |  9.88 |    0.90 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  13.59 ns | 27.17 ns | 1.489 ns |  1.01 |    0.13 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 598.32 ns | 65.13 ns | 3.570 ns | 44.35 |    4.02 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 592.72 ns | 91.14 ns | 4.996 ns | 43.94 |    3.99 | 0.0601 |    1008 B |       42.00 |
