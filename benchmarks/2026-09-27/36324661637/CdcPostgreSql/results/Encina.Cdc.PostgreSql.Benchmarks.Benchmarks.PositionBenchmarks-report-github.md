```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.312 ns | 0.1170 ns | 0.0064 ns |  0.12 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 10.516 ns | 3.3638 ns | 0.1844 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  7.056 ns | 1.8081 ns | 0.0991 ns |  0.67 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  8.283 ns | 2.6293 ns | 0.1441 ns |  0.79 |    0.02 | 0.0019 |      32 B |        1.33 |
