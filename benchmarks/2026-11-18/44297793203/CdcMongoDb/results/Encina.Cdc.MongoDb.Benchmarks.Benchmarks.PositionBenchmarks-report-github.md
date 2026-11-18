```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 103.871 ns |  3.5075 ns | 0.1923 ns | 13.76 |    0.05 | 0.0011 |      96 B |        4.00 |
| CreatePosition   |   7.548 ns |  0.4779 ns | 0.0262 ns |  1.00 |    0.00 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 468.851 ns | 11.0432 ns | 0.6053 ns | 62.12 |    0.20 | 0.0119 |    1024 B |       42.67 |
| ToBytes          | 423.285 ns | 27.8682 ns | 1.5276 ns | 56.08 |    0.24 | 0.0119 |    1008 B |       42.00 |
