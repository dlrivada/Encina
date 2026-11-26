```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 135.545 ns |  29.988 ns |  1.6437 ns | 16.21 |    0.33 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.363 ns |   3.092 ns |  0.1695 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 606.064 ns | 132.443 ns |  7.2596 ns | 72.49 |    1.49 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 609.801 ns | 910.138 ns | 49.8877 ns | 72.94 |    5.33 | 0.0601 |    1008 B |       42.00 |
