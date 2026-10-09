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
| ComparePositions | 1.324 ns | 0.1220 ns | 0.0067 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 9.296 ns | 9.9567 ns | 0.5458 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.265 ns | 0.4603 ns | 0.0252 ns |  0.68 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.717 ns | 0.2965 ns | 0.0163 ns |  0.72 |    0.04 | 0.0019 |      32 B |        1.33 |
