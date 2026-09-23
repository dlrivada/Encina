```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.307 ns | 0.1176 ns | 0.0064 ns |  0.12 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 10.916 ns | 3.5091 ns | 0.1923 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.749 ns | 2.9216 ns | 0.1601 ns |  0.62 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.370 ns | 2.7218 ns | 0.1492 ns |  0.68 |    0.02 | 0.0019 |      32 B |        1.33 |
