```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.274 ns | 0.0801 ns | 0.0044 ns |  0.19 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.673 ns | 1.0610 ns | 0.0582 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 15.943 ns | 4.7885 ns | 0.2625 ns |  2.39 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 16.309 ns | 6.7210 ns | 0.3684 ns |  2.44 |    0.05 | 0.0019 |      32 B |        1.33 |
