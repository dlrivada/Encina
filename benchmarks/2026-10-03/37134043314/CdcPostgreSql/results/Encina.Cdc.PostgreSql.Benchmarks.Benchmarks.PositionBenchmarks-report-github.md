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
| ComparePositions | 1.337 ns | 0.0332 ns | 0.0018 ns |  0.23 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.809 ns | 0.2511 ns | 0.0138 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 8.061 ns | 1.4496 ns | 0.0795 ns |  1.39 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.033 ns | 9.8084 ns | 0.5376 ns |  1.21 |    0.08 | 0.0019 |      32 B |        1.33 |
