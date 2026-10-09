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
| ComparePositions |  1.311 ns | 0.0292 ns | 0.0016 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 10.488 ns | 7.4175 ns | 0.4066 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 17.480 ns | 2.7439 ns | 0.1504 ns |  1.67 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  8.597 ns | 0.7424 ns | 0.0407 ns |  0.82 |    0.03 | 0.0019 |      32 B |        1.33 |
