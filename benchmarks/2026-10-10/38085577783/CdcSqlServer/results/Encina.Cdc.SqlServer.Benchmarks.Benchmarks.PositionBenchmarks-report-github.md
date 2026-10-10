```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.8701 ns | 0.0262 ns | 0.0014 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.9373 ns | 1.9139 ns | 0.1049 ns |  1.00 |    0.02 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 6.8624 ns | 0.1871 ns | 0.0103 ns |  1.16 |    0.02 | 0.0010 |      24 B |        1.00 |
| ToBytes          | 7.2026 ns | 1.8117 ns | 0.0993 ns |  1.21 |    0.02 | 0.0013 |      32 B |        1.33 |
