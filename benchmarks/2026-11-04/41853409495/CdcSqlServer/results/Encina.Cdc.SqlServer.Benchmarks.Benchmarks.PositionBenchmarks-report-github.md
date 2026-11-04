```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.319 ns |  1.262 ns | 0.0692 ns |  0.19 |    0.01 |      - |         - |        0.00 |
| CreatePosition   |  6.984 ns |  5.462 ns | 0.2994 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 12.346 ns | 11.302 ns | 0.6195 ns |  1.77 |    0.10 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.812 ns |  2.538 ns | 0.1391 ns |  0.98 |    0.04 | 0.0019 |      32 B |        1.33 |
