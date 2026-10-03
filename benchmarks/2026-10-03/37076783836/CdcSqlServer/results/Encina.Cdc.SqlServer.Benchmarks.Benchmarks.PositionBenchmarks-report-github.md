```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.8412 ns | 0.0580 ns | 0.0032 ns |  0.19 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.5204 ns | 3.6832 ns | 0.2019 ns |  1.00 |    0.06 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 6.4185 ns | 1.9191 ns | 0.1052 ns |  1.42 |    0.06 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 6.5598 ns | 3.9342 ns | 0.2156 ns |  1.45 |    0.07 | 0.0004 |      32 B |        1.33 |
