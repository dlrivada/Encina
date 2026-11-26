```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 138.890 ns |  23.687 ns | 1.2984 ns | 16.11 |    0.16 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.621 ns |   1.051 ns | 0.0576 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 623.135 ns | 179.020 ns | 9.8127 ns | 72.28 |    1.07 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 568.129 ns |  33.642 ns | 1.8440 ns | 65.90 |    0.42 | 0.0601 |    1008 B |       42.00 |
