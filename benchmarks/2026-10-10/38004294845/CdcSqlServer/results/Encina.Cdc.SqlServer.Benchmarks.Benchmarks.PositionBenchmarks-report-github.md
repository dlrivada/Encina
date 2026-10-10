```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.402 ns |  0.0054 ns | 0.0003 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 10.102 ns | 10.7093 ns | 0.5870 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.905 ns |  0.6925 ns | 0.0380 ns |  0.69 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.831 ns |  0.6011 ns | 0.0330 ns |  0.68 |    0.03 | 0.0019 |      32 B |        1.33 |
