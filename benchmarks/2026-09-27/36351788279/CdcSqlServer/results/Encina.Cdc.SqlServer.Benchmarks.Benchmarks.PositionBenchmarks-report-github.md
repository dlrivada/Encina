```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.8370 ns | 0.2497 ns | 0.0137 ns |  0.16 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.1226 ns | 3.1653 ns | 0.1735 ns |  1.00 |    0.04 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 5.8821 ns | 2.2783 ns | 0.1249 ns |  1.15 |    0.04 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 5.9323 ns | 0.2182 ns | 0.0120 ns |  1.16 |    0.03 | 0.0004 |      32 B |        1.33 |
