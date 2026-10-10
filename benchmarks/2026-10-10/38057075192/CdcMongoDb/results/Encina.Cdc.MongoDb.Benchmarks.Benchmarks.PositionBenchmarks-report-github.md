```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.52 ns |   7.672 ns | 0.421 ns |  9.69 |    0.60 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  13.93 ns |  17.445 ns | 0.956 ns |  1.00 |    0.09 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 627.88 ns | 150.745 ns | 8.263 ns | 45.23 |    2.84 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 608.23 ns |  94.437 ns | 5.176 ns | 43.81 |    2.73 | 0.0601 |    1008 B |       42.00 |
