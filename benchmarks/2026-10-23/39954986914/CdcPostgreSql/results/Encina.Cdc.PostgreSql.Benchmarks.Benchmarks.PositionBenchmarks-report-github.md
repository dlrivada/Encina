```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.312 ns | 0.0273 ns | 0.0015 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 10.007 ns | 4.5742 ns | 0.2507 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  7.481 ns | 2.0816 ns | 0.1141 ns |  0.75 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.483 ns | 2.2724 ns | 0.1246 ns |  0.75 |    0.02 | 0.0019 |      32 B |        1.33 |
