```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions |  1.275 ns | 0.0491 ns | 0.0027 ns |  0.13 |      - |         - |        0.00 |
| CreatePosition   | 10.127 ns | 1.4015 ns | 0.0768 ns |  1.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.603 ns | 0.9138 ns | 0.0501 ns |  0.65 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.114 ns | 1.3096 ns | 0.0718 ns |  0.60 | 0.0019 |      32 B |        1.33 |
