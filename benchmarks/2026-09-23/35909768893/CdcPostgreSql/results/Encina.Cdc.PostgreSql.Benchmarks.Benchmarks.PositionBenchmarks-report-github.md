```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.7365 ns | 1.9210 ns | 0.1053 ns |  0.15 |    0.02 |      - |         - |        0.00 |
| CreatePosition   | 4.7651 ns | 2.1062 ns | 0.1154 ns |  1.00 |    0.03 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 6.2352 ns | 0.5899 ns | 0.0323 ns |  1.31 |    0.03 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.2773 ns | 4.3048 ns | 0.2360 ns |  1.53 |    0.05 | 0.0004 |      32 B |        1.33 |
