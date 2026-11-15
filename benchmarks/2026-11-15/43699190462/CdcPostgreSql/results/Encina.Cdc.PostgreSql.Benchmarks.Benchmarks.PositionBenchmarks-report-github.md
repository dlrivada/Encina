```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.090 ns | 2.0699 ns | 0.1135 ns |  0.23 |    0.02 |      - |         - |        0.00 |
| CreatePosition   | 4.808 ns | 3.8946 ns | 0.2135 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.063 ns | 0.7269 ns | 0.0398 ns |  1.26 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.761 ns | 2.8783 ns | 0.1578 ns |  1.20 |    0.05 | 0.0019 |      32 B |        1.33 |
