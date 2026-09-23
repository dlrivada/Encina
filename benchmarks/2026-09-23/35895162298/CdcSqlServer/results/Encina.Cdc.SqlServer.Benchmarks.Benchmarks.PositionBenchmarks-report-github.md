```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.317 ns | 0.0853 ns | 0.0047 ns |  0.22 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.910 ns | 3.6933 ns | 0.2024 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 8.589 ns | 0.4394 ns | 0.0241 ns |  1.45 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.668 ns | 4.0600 ns | 0.2225 ns |  1.13 |    0.05 | 0.0019 |      32 B |        1.33 |
