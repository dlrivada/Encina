```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 136.80 ns | 0.362 ns |  0.519 ns |  9.80 |    1.45 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  14.27 ns | 1.455 ns |  2.177 ns |  1.02 |    0.22 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 662.59 ns | 6.042 ns |  9.043 ns | 47.49 |    7.06 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 569.26 ns | 8.572 ns | 12.294 ns | 40.80 |    6.11 | 0.0601 |    1008 B |       42.00 |
