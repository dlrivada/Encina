```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 137.61 ns | 0.953 ns |  1.397 ns | 13.01 |    1.76 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  10.76 ns | 0.989 ns |  1.418 ns |  1.02 |    0.19 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 609.59 ns | 8.660 ns | 12.962 ns | 57.65 |    7.85 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 557.30 ns | 2.743 ns |  3.845 ns | 52.70 |    7.10 | 0.0601 |    1008 B |       42.00 |
