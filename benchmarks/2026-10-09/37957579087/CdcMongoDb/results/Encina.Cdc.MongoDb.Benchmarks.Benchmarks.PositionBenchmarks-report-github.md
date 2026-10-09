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
| ComparePositions | 135.15 ns | 18.02 ns | 0.988 ns |  7.70 |    0.49 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  17.62 ns | 24.73 ns | 1.355 ns |  1.00 |    0.09 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 596.33 ns | 30.58 ns | 1.676 ns | 33.97 |    2.17 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 581.62 ns | 17.14 ns | 0.940 ns | 33.14 |    2.12 | 0.0601 |    1008 B |       42.00 |
