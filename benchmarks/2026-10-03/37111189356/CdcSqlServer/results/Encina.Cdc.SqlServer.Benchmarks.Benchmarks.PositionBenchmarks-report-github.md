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
| ComparePositions | 0.6311 ns |  0.0103 ns | 0.0006 ns |  0.12 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.4597 ns |  0.7187 ns | 0.0394 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.3728 ns | 10.2911 ns | 0.5641 ns |  1.35 |    0.09 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 5.0989 ns |  0.8031 ns | 0.0440 ns |  0.93 |    0.01 | 0.0004 |      32 B |        1.33 |
