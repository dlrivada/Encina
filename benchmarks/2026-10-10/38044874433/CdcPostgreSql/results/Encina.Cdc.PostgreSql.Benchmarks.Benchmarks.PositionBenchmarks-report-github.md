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
| ComparePositions | 1.119 ns | 0.3510 ns | 0.0192 ns |  0.24 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 4.686 ns | 0.3038 ns | 0.0167 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.817 ns | 0.2117 ns | 0.0116 ns |  1.24 |    0.00 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.606 ns | 2.6528 ns | 0.1454 ns |  1.20 |    0.03 | 0.0019 |      32 B |        1.33 |
