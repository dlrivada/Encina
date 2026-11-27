```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 146.451 ns |  33.2692 ns | 1.8236 ns | 18.72 |    0.21 | 0.0010 |      96 B |        4.00 |
| CreatePosition   |   7.822 ns |   0.5205 ns | 0.0285 ns |  1.00 |    0.00 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 595.932 ns | 122.4247 ns | 6.7105 ns | 76.19 |    0.78 | 0.0114 |    1024 B |       42.67 |
| ToBytes          | 547.261 ns |  21.9690 ns | 1.2042 ns | 69.96 |    0.26 | 0.0114 |    1008 B |       42.00 |
