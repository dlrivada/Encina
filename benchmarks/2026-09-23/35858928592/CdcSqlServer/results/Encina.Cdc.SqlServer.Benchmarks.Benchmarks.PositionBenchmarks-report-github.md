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
| ComparePositions | 1.063 ns | 0.0390 ns | 0.0021 ns |  0.23 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.622 ns | 4.1953 ns | 0.2300 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.075 ns | 2.3592 ns | 0.1293 ns |  1.10 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.226 ns | 0.8141 ns | 0.0446 ns |  1.13 |    0.05 | 0.0019 |      32 B |        1.33 |
