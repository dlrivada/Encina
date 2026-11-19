```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.6428 ns | 0.2284 ns | 0.0125 ns |  0.18 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.5078 ns | 1.1480 ns | 0.0629 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 4.0043 ns | 1.3427 ns | 0.0736 ns |  1.14 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.8176 ns | 1.1961 ns | 0.0656 ns |  1.09 |    0.02 | 0.0019 |      32 B |        1.33 |
