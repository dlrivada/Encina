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
| ComparePositions | 1.280 ns | 0.1165 ns | 0.0064 ns |  0.16 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 8.207 ns | 6.4557 ns | 0.3539 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.442 ns | 0.8856 ns | 0.0485 ns |  0.79 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.379 ns | 2.5119 ns | 0.1377 ns |  0.78 |    0.03 | 0.0019 |      32 B |        1.33 |
