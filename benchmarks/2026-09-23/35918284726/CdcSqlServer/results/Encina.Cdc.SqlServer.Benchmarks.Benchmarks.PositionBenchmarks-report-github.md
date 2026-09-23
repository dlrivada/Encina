```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.065 ns | 0.1518 ns | 0.0083 ns |  0.25 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.274 ns | 2.4109 ns | 0.1321 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.711 ns | 2.2164 ns | 0.1215 ns |  1.34 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.185 ns | 0.8150 ns | 0.0447 ns |  1.21 |    0.03 | 0.0019 |      32 B |        1.33 |
