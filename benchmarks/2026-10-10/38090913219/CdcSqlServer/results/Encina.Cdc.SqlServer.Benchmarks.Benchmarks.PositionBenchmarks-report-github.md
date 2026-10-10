```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.4710 ns | 0.1722 ns | 0.0094 ns |  0.15 |      - |         - |        0.00 |
| CreatePosition   | 3.1882 ns | 0.2402 ns | 0.0132 ns |  1.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.4289 ns | 0.0132 ns | 0.0007 ns |  1.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.2873 ns | 0.4253 ns | 0.0233 ns |  1.03 | 0.0019 |      32 B |        1.33 |
