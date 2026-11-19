```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 106.637 ns |   4.065 ns |  0.2228 ns | 14.64 |    0.92 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   7.313 ns |   9.990 ns |  0.5476 ns |  1.00 |    0.09 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 515.127 ns | 285.705 ns | 15.6605 ns | 70.70 |    4.82 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 477.735 ns | 123.247 ns |  6.7556 ns | 65.57 |    4.20 | 0.0601 |    1008 B |       42.00 |
