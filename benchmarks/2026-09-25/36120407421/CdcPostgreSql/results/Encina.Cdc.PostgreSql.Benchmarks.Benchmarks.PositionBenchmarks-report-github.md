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
| ComparePositions | 1.024 ns | 0.0947 ns | 0.0052 ns |  0.21 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.877 ns | 3.1202 ns | 0.1710 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.438 ns | 1.6530 ns | 0.0906 ns |  1.32 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.065 ns | 5.3821 ns | 0.2950 ns |  1.24 |    0.06 | 0.0019 |      32 B |        1.33 |
