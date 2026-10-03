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
| ComparePositions | 0.6310 ns | 0.0041 ns | 0.0002 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 4.9040 ns | 1.8747 ns | 0.1028 ns |  1.00 |    0.03 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 5.2237 ns | 4.9743 ns | 0.2727 ns |  1.07 |    0.05 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 4.6819 ns | 2.2040 ns | 0.1208 ns |  0.96 |    0.03 | 0.0004 |      32 B |        1.33 |
