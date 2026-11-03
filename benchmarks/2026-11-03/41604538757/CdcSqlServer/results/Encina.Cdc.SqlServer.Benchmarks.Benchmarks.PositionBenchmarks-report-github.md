```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4848 ns | 0.3336 ns | 0.0183 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.4606 ns | 1.1723 ns | 0.0643 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.6469 ns | 0.5898 ns | 0.0323 ns |  1.05 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.3805 ns | 3.5026 ns | 0.1920 ns |  0.98 |    0.05 | 0.0019 |      32 B |        1.33 |
