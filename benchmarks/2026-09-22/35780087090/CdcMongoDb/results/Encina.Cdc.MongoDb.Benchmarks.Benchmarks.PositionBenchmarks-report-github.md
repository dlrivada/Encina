```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.00 ns |   5.483 ns | 0.301 ns | 10.53 |    1.27 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  12.90 ns |  34.832 ns | 1.909 ns |  1.01 |    0.18 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 609.17 ns | 145.367 ns | 7.968 ns | 47.88 |    5.82 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 587.31 ns | 141.753 ns | 7.770 ns | 46.16 |    5.61 | 0.0601 |    1008 B |       42.00 |
