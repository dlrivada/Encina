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
| ComparePositions | 1.274 ns | 0.0263 ns | 0.0014 ns |  0.14 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 9.029 ns | 4.9905 ns | 0.2735 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.375 ns | 0.9490 ns | 0.0520 ns |  0.82 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.048 ns | 0.1220 ns | 0.0067 ns |  0.78 |    0.02 | 0.0019 |      32 B |        1.33 |
