```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 139.61 ns |   5.717 ns |  0.313 ns | 13.17 |    0.21 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  10.61 ns |   3.492 ns |  0.191 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 652.19 ns | 223.613 ns | 12.257 ns | 61.50 |    1.39 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 637.99 ns | 201.627 ns | 11.052 ns | 60.16 |    1.31 | 0.0601 |    1008 B |       42.00 |
