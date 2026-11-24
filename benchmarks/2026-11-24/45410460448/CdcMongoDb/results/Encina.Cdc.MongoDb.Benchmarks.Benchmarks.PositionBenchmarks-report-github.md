```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 107.418 ns | 13.765 ns | 0.7545 ns | 16.48 |    0.25 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.521 ns |  1.942 ns | 0.1064 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 500.150 ns | 58.246 ns | 3.1927 ns | 76.71 |    1.17 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 464.136 ns | 52.381 ns | 2.8712 ns | 71.19 |    1.08 | 0.0601 |    1008 B |       42.00 |
