```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.300 ns | 0.0028 ns | 0.0040 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 9.340 ns | 0.2797 ns | 0.4187 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.296 ns | 0.0182 ns | 0.0267 ns |  0.68 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.743 ns | 0.0218 ns | 0.0306 ns |  0.72 |    0.03 | 0.0019 |      32 B |        1.33 |
