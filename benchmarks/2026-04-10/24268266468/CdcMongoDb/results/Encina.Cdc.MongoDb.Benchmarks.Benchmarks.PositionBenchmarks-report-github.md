```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 137.25 ns |  11.050 ns | 0.606 ns | 11.41 |    0.44 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  12.04 ns |   9.539 ns | 0.523 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 625.18 ns | 178.167 ns | 9.766 ns | 51.99 |    2.11 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 570.09 ns |  43.005 ns | 2.357 ns | 47.41 |    1.83 | 0.0601 |    1008 B |       42.00 |
