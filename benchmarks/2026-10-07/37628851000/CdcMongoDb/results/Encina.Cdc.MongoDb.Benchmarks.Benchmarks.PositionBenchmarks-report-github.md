```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 107.982 ns |  10.2747 ns |  0.5632 ns | 17.39 |    0.12 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.208 ns |   0.6698 ns |  0.0367 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 459.265 ns | 219.1448 ns | 12.0121 ns | 73.98 |    1.72 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 432.649 ns |  35.2887 ns |  1.9343 ns | 69.69 |    0.45 | 0.0601 |    1008 B |       42.00 |
