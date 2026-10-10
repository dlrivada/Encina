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
| ComparePositions |  1.279 ns | 0.0404 ns | 0.0022 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 10.175 ns | 6.0255 ns | 0.3303 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  7.560 ns | 0.5268 ns | 0.0289 ns |  0.74 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.244 ns | 0.4585 ns | 0.0251 ns |  0.71 |    0.02 | 0.0019 |      32 B |        1.33 |
