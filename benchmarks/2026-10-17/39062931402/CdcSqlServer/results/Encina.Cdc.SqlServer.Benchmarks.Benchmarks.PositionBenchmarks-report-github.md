```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.075 ns | 0.0484 ns | 0.0027 ns |  0.21 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.090 ns | 3.8880 ns | 0.2131 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.084 ns | 3.0001 ns | 0.1644 ns |  1.20 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.225 ns | 3.1755 ns | 0.1741 ns |  1.03 |    0.05 | 0.0019 |      32 B |        1.33 |
