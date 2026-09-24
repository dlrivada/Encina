```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.6321 ns |  0.0220 ns | 0.0012 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 4.1560 ns |  0.4868 ns | 0.0267 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 5.3559 ns |  7.4231 ns | 0.4069 ns |  1.29 |    0.09 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 6.4136 ns | 14.6211 ns | 0.8014 ns |  1.54 |    0.17 | 0.0004 |      32 B |        1.33 |
