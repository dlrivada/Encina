```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.92GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  98.946 ns |   3.350 ns | 0.1837 ns | 14.31 |    0.41 | 0.0011 |      96 B |        4.00 |
| CreatePosition   |   6.922 ns |   4.237 ns | 0.2322 ns |  1.00 |    0.04 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 437.228 ns | 121.837 ns | 6.6783 ns | 63.21 |    1.99 | 0.0119 |    1024 B |       42.67 |
| ToBytes          | 399.922 ns | 157.369 ns | 8.6259 ns | 57.82 |    1.98 | 0.0119 |    1008 B |       42.00 |
