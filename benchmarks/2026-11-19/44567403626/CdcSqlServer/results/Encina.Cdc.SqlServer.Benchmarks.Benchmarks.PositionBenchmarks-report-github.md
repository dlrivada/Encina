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
| ComparePositions | 1.076 ns | 0.2921 ns | 0.0160 ns |  0.21 |    0.02 |      - |         - |        0.00 |
| CreatePosition   | 5.124 ns | 7.3040 ns | 0.4004 ns |  1.00 |    0.10 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.841 ns | 1.9232 ns | 0.1054 ns |  1.14 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.901 ns | 0.4465 ns | 0.0245 ns |  1.16 |    0.08 | 0.0019 |      32 B |        1.33 |
