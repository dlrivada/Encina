```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.019 ns | 0.3296 ns | 0.0181 ns |  0.20 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.030 ns | 3.0948 ns | 0.1696 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.294 ns | 2.1965 ns | 0.1204 ns |  1.25 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.188 ns | 2.4409 ns | 0.1338 ns |  1.23 |    0.04 | 0.0019 |      32 B |        1.33 |
