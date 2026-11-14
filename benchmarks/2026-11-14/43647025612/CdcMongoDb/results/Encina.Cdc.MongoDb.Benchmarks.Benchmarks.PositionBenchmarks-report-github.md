```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.81GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 139.457 ns |  16.5341 ns | 0.9063 ns | 17.13 |    0.13 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.140 ns |   0.8524 ns | 0.0467 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 611.611 ns |  79.2476 ns | 4.3438 ns | 75.14 |    0.59 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 581.700 ns | 118.7693 ns | 6.5101 ns | 71.46 |    0.78 | 0.0601 |    1008 B |       42.00 |
