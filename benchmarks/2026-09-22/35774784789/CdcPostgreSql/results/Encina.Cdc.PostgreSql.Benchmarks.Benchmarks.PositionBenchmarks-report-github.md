```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.330 ns |  0.3464 ns | 0.0190 ns |  0.12 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 11.081 ns | 12.7433 ns | 0.6985 ns |  1.00 |    0.08 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  7.012 ns |  5.1943 ns | 0.2847 ns |  0.63 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.473 ns |  1.9716 ns | 0.1081 ns |  0.68 |    0.04 | 0.0019 |      32 B |        1.33 |
