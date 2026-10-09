```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.009 ns | 0.0129 ns | 0.0007 ns |  0.19 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.435 ns | 6.1537 ns | 0.3373 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.211 ns | 6.7127 ns | 0.3679 ns |  1.15 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.083 ns | 3.3587 ns | 0.1841 ns |  1.12 |    0.07 | 0.0019 |      32 B |        1.33 |
