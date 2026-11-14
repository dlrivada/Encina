```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 107.954 ns |  1.7831 ns | 0.0977 ns | 14.16 |    0.02 | 0.0011 |      96 B |        4.00 |
| CreatePosition   |   7.624 ns |  0.1171 ns | 0.0064 ns |  1.00 |    0.00 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 474.376 ns | 40.0424 ns | 2.1949 ns | 62.22 |    0.25 | 0.0119 |    1024 B |       42.67 |
| ToBytes          | 462.664 ns | 47.1601 ns | 2.5850 ns | 60.68 |    0.30 | 0.0119 |    1008 B |       42.00 |
