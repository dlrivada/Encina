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
| ComparePositions | 141.43 ns |   3.569 ns | 0.196 ns |  9.48 |    0.49 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  14.96 ns |  16.864 ns | 0.924 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 639.75 ns |  66.070 ns | 3.622 ns | 42.88 |    2.23 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 653.67 ns | 106.145 ns | 5.818 ns | 43.81 |    2.29 | 0.0601 |    1008 B |       42.00 |
