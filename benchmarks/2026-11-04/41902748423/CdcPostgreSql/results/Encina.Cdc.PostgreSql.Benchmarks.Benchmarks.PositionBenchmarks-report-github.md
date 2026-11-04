```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.022 ns | 0.0887 ns | 0.0049 ns |  0.21 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.930 ns | 2.8217 ns | 0.1547 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.868 ns | 0.6377 ns | 0.0350 ns |  1.19 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.979 ns | 0.4449 ns | 0.0244 ns |  1.21 |    0.03 | 0.0019 |      32 B |        1.33 |
