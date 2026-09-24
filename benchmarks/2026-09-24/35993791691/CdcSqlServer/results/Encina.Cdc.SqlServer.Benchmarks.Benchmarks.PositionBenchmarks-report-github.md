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
| ComparePositions | 1.281 ns | 0.0848 ns | 0.0046 ns |  0.14 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 9.108 ns | 6.1538 ns | 0.3373 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.940 ns | 1.1545 ns | 0.0633 ns |  0.76 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.390 ns | 1.0154 ns | 0.0557 ns |  0.70 |    0.02 | 0.0019 |      32 B |        1.33 |
