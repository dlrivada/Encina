```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.79GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 136.44 ns |  10.89 ns | 0.597 ns |  7.25 |    0.36 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  18.86 ns |  20.17 ns | 1.106 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 652.45 ns |  11.89 ns | 0.652 ns | 34.67 |    1.70 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 604.63 ns | 114.09 ns | 6.253 ns | 32.13 |    1.61 | 0.0601 |    1008 B |       42.00 |
