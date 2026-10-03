```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.7284 ns | 0.1963 ns | 0.0108 ns |  0.09 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 7.7264 ns | 1.8696 ns | 0.1025 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.8146 ns | 0.7745 ns | 0.0425 ns |  1.01 |    0.01 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 8.4597 ns | 1.8804 ns | 0.1031 ns |  1.10 |    0.02 | 0.0004 |      32 B |        1.33 |
