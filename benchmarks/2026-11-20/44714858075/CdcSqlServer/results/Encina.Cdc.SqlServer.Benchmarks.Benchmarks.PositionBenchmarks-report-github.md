```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.269 ns | 0.0732 ns | 0.0040 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.422 ns | 3.2587 ns | 0.1786 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.906 ns | 0.8472 ns | 0.0464 ns |  1.23 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.156 ns | 1.9123 ns | 0.1048 ns |  1.11 |    0.03 | 0.0019 |      32 B |        1.33 |
