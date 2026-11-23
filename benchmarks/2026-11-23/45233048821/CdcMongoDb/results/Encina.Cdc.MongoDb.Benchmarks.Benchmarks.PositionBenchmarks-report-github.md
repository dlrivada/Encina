```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 108.214 ns |  2.299 ns | 0.1260 ns | 13.76 |    0.84 | 0.0011 |      96 B |        4.00 |
| CreatePosition   |   7.887 ns |  9.696 ns | 0.5315 ns |  1.00 |    0.08 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 477.614 ns | 28.765 ns | 1.5767 ns | 60.75 |    3.69 | 0.0119 |    1024 B |       42.67 |
| ToBytes          | 463.101 ns | 29.648 ns | 1.6251 ns | 58.91 |    3.58 | 0.0119 |    1008 B |       42.00 |
