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
| ComparePositions | 1.032 ns | 0.1216 ns | 0.0067 ns |  0.22 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.603 ns | 2.4490 ns | 0.1342 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.699 ns | 2.6613 ns | 0.1459 ns |  1.24 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.539 ns | 2.5839 ns | 0.1416 ns |  1.20 |    0.04 | 0.0019 |      32 B |        1.33 |
