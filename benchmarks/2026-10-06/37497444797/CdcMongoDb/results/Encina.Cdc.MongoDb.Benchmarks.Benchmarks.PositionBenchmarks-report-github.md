```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.71GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.88 ns |  7.577 ns | 0.415 ns |  8.07 |    0.20 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  16.72 ns |  8.880 ns | 0.487 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 613.99 ns | 42.968 ns | 2.355 ns | 36.74 |    0.92 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 660.30 ns | 92.351 ns | 5.062 ns | 39.52 |    1.02 | 0.0601 |    1008 B |       42.00 |
