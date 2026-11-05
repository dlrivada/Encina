```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4490 ns | 0.1234 ns | 0.0068 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.5664 ns | 1.1879 ns | 0.0651 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 4.3015 ns | 1.7688 ns | 0.0970 ns |  1.21 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.3533 ns | 1.3645 ns | 0.0748 ns |  0.94 |    0.02 | 0.0019 |      32 B |        1.33 |
