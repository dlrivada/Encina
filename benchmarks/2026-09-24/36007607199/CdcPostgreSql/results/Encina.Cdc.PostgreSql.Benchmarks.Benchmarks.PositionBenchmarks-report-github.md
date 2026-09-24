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
| ComparePositions | 1.338 ns | 0.0357 ns | 0.0020 ns |  0.22 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.958 ns | 4.0378 ns | 0.2213 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.137 ns | 1.2194 ns | 0.0668 ns |  1.20 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.666 ns | 7.1900 ns | 0.3941 ns |  1.29 |    0.07 | 0.0019 |      32 B |        1.33 |
