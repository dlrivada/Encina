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
| ComparePositions | 1.659 ns | 0.0690 ns | 0.0038 ns |  0.18 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 9.356 ns | 5.2752 ns | 0.2892 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.242 ns | 0.2906 ns | 0.0159 ns |  0.67 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.800 ns | 0.7150 ns | 0.0392 ns |  0.73 |    0.02 | 0.0019 |      32 B |        1.33 |
