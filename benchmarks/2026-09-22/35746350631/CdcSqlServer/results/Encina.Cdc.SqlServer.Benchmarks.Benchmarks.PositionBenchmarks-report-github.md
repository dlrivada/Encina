```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 1.274 ns | 0.0151 ns | 0.0008 ns |  0.15 |      - |         - |        0.00 |
| CreatePosition   | 8.514 ns | 0.9002 ns | 0.0493 ns |  1.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.474 ns | 1.8212 ns | 0.0998 ns |  0.88 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.112 ns | 1.2065 ns | 0.0661 ns |  0.84 | 0.0019 |      32 B |        1.33 |
