```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 150.738 ns |  3.231 ns | 0.1771 ns | 18.55 |    0.12 | 0.0010 |      96 B |        4.00 |
| CreatePosition   |   8.125 ns |  1.112 ns | 0.0610 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 608.242 ns | 82.457 ns | 4.5197 ns | 74.87 |    0.68 | 0.0114 |    1024 B |       42.67 |
| ToBytes          | 558.636 ns | 23.358 ns | 1.2803 ns | 68.76 |    0.47 | 0.0114 |    1008 B |       42.00 |
