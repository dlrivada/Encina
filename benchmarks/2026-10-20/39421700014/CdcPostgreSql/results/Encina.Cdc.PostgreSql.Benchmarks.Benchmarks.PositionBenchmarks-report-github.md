```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.321 ns |  0.1485 ns | 0.0081 ns |  0.23 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  5.624 ns |  0.6057 ns | 0.0332 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 17.981 ns | 31.1956 ns | 1.7099 ns |  3.20 |    0.26 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.265 ns |  1.2012 ns | 0.0658 ns |  1.29 |    0.01 | 0.0019 |      32 B |        1.33 |
