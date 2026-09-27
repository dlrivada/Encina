```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 131.68 ns |  4.733 ns | 0.259 ns | 11.49 |    1.28 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  11.60 ns | 29.379 ns | 1.610 ns |  1.01 |    0.17 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 603.32 ns | 57.583 ns | 3.156 ns | 52.65 |    5.87 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 614.14 ns | 68.997 ns | 3.782 ns | 53.60 |    5.98 | 0.0601 |    1008 B |       42.00 |
