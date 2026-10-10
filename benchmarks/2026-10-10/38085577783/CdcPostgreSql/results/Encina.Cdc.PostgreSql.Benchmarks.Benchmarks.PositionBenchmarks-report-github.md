```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4621 ns | 0.2637 ns | 0.0145 ns |  0.13 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.5594 ns | 4.7281 ns | 0.2592 ns |  1.00 |    0.09 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.6263 ns | 5.8354 ns | 0.3199 ns |  1.02 |    0.10 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.9269 ns | 4.0112 ns | 0.2199 ns |  1.11 |    0.09 | 0.0019 |      32 B |        1.33 |
