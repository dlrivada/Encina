```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.339 ns | 0.0826 ns | 0.0045 ns |  0.22 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.092 ns | 1.6487 ns | 0.0904 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.582 ns | 2.6579 ns | 0.1457 ns |  1.08 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.182 ns | 2.4261 ns | 0.1330 ns |  1.18 |    0.02 | 0.0019 |      32 B |        1.33 |
