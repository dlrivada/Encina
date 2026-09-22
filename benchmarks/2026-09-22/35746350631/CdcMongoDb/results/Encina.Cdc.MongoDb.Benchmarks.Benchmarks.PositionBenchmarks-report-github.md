```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 135.972 ns |   9.2579 ns |  0.5075 ns | 16.59 |    0.06 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.195 ns |   0.3064 ns |  0.0168 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 572.804 ns |   4.3328 ns |  0.2375 ns | 69.90 |    0.13 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 553.460 ns | 240.6664 ns | 13.1917 ns | 67.54 |    1.40 | 0.0601 |    1008 B |       42.00 |
