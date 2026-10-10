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
| ComparePositions | 0.8734 ns | 0.0306 ns | 0.0017 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.5427 ns | 0.8612 ns | 0.0472 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.4657 ns | 1.7339 ns | 0.0950 ns |  1.14 |    0.01 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.7171 ns | 2.2025 ns | 0.1207 ns |  1.18 |    0.02 | 0.0004 |      32 B |        1.33 |
