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
| ComparePositions | 1.055 ns | 0.4726 ns | 0.0259 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.405 ns | 1.2897 ns | 0.0707 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.902 ns | 1.3124 ns | 0.0719 ns |  1.28 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.708 ns | 2.1944 ns | 0.1203 ns |  1.24 |    0.02 | 0.0019 |      32 B |        1.33 |
