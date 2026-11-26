```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 150.556 ns |  8.302 ns | 0.4551 ns | 16.64 |    0.22 | 0.0010 |      96 B |        4.00 |
| CreatePosition   |   9.050 ns |  2.482 ns | 0.1361 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 611.600 ns | 59.550 ns | 3.2641 ns | 67.59 |    0.94 | 0.0114 |    1024 B |       42.67 |
| ToBytes          | 574.864 ns |  5.890 ns | 0.3228 ns | 63.53 |    0.83 | 0.0114 |    1008 B |       42.00 |
