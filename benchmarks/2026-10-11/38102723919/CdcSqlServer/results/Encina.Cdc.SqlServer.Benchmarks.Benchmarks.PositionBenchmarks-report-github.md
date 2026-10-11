```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.437 ns | 0.1122 ns | 0.1572 ns | 1.296 ns |  0.16 |    0.02 |      - |         - |        0.00 |
| CreatePosition   | 8.993 ns | 0.2519 ns | 0.3692 ns | 9.065 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.898 ns | 0.2333 ns | 0.3492 ns | 6.933 ns |  0.77 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.418 ns | 0.1269 ns | 0.1900 ns | 6.398 ns |  0.71 |    0.04 | 0.0019 |      32 B |        1.33 |
