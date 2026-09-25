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
| ComparePositions | 135.28 ns |  1.776 ns | 0.097 ns | 10.06 |    0.68 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  13.51 ns | 19.999 ns | 1.096 ns |  1.00 |    0.10 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 621.46 ns | 35.727 ns | 1.958 ns | 46.20 |    3.11 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 618.84 ns | 33.075 ns | 1.813 ns | 46.01 |    3.09 | 0.0601 |    1008 B |       42.00 |
