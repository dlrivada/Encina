```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4985 ns | 0.6072 ns | 0.0333 ns |  0.15 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.4112 ns | 2.9443 ns | 0.1614 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.4272 ns | 1.3398 ns | 0.0734 ns |  1.01 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.8643 ns | 2.3625 ns | 0.1295 ns |  1.13 |    0.06 | 0.0019 |      32 B |        1.33 |
