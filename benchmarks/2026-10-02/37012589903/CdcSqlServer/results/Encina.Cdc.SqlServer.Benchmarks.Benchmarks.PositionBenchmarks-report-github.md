```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.281 ns | 0.0482 ns | 0.0026 ns |  0.13 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 10.117 ns | 8.5591 ns | 0.4692 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  7.248 ns | 2.2913 ns | 0.1256 ns |  0.72 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.600 ns | 1.1771 ns | 0.0645 ns |  0.65 |    0.03 | 0.0019 |      32 B |        1.33 |
