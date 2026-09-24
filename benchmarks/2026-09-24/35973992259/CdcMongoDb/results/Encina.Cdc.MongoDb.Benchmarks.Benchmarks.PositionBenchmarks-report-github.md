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
| ComparePositions | 130.77 ns | 10.83 ns | 0.594 ns |  9.63 |    1.28 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  13.77 ns | 35.32 ns | 1.936 ns |  1.01 |    0.18 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 588.16 ns | 49.14 ns | 2.693 ns | 43.33 |    5.74 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 567.75 ns | 22.47 ns | 1.232 ns | 41.83 |    5.54 | 0.0601 |    1008 B |       42.00 |
