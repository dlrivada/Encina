```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.024 ns | 0.0732 ns | 0.0040 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.094 ns | 0.2346 ns | 0.0129 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.371 ns | 6.5711 ns | 0.3602 ns |  1.25 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.131 ns | 1.1951 ns | 0.0655 ns |  1.20 |    0.01 | 0.0019 |      32 B |        1.33 |
