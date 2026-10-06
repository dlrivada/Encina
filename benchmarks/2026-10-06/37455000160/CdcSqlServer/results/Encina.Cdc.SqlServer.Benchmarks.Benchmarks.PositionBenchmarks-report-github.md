```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.057 ns | 0.0307 ns | 0.0017 ns |  0.21 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.955 ns | 5.6479 ns | 0.3096 ns |  1.00 |    0.08 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.610 ns | 1.0589 ns | 0.0580 ns |  1.34 |    0.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.612 ns | 3.6828 ns | 0.2019 ns |  1.14 |    0.07 | 0.0019 |      32 B |        1.33 |
