```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.275 ns | 0.0370 ns | 0.0020 ns |  0.23 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.584 ns | 3.2890 ns | 0.1803 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.644 ns | 1.1785 ns | 0.0646 ns |  1.19 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.046 ns | 1.0035 ns | 0.0550 ns |  1.08 |    0.03 | 0.0019 |      32 B |        1.33 |
