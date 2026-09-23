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
| ComparePositions |  1.321 ns |  0.1764 ns | 0.0097 ns |  0.11 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 11.772 ns |  5.2738 ns | 0.2891 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.067 ns |  2.3446 ns | 0.1285 ns |  0.52 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 17.481 ns | 34.1311 ns | 1.8708 ns |  1.49 |    0.14 | 0.0019 |      32 B |        1.33 |
