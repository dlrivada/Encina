```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  74.288 ns |  12.420 ns |  0.6808 ns | 16.02 |    0.85 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.647 ns |   4.956 ns |  0.2717 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 326.367 ns |  41.826 ns |  2.2926 ns | 70.39 |    3.70 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 326.934 ns | 317.328 ns | 17.3938 ns | 70.51 |    4.91 | 0.0601 |    1008 B |       42.00 |
