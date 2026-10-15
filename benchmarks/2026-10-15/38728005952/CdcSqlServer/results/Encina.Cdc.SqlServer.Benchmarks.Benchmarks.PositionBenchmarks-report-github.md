```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.637 ns | 0.1512 ns | 0.0083 ns |  0.18 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 9.009 ns | 7.9356 ns | 0.4350 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.309 ns | 1.2577 ns | 0.0689 ns |  0.81 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.367 ns | 2.1926 ns | 0.1202 ns |  0.71 |    0.03 | 0.0019 |      32 B |        1.33 |
