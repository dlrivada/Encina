```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.460 ns |   9.934 ns |  0.5445 ns | 16.09 |    0.12 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.359 ns |   1.130 ns |  0.0620 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 626.750 ns | 203.185 ns | 11.1372 ns | 74.98 |    1.25 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 579.236 ns |  60.589 ns |  3.3211 ns | 69.30 |    0.56 | 0.0601 |    1008 B |       42.00 |
