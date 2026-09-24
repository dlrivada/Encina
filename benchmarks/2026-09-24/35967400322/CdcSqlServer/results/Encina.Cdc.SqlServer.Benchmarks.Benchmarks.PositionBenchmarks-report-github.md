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
| ComparePositions | 0.6659 ns |  0.6669 ns | 0.0366 ns |  0.09 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 7.3353 ns | 12.0924 ns | 0.6628 ns |  1.01 |    0.11 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.3361 ns |  2.5432 ns | 0.1394 ns |  1.01 |    0.08 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.1127 ns |  7.1522 ns | 0.3920 ns |  0.97 |    0.09 | 0.0004 |      32 B |        1.33 |
