```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.42GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4267 ns | 0.6013 ns | 0.0330 ns |  0.12 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.5319 ns | 2.1386 ns | 0.1172 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.8366 ns | 5.7747 ns | 0.3165 ns |  1.09 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.2416 ns | 1.3328 ns | 0.0731 ns |  0.92 |    0.03 | 0.0019 |      32 B |        1.33 |
